import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

export interface ConsentChoices {
  analytics: boolean;
  marketing: boolean;
}

interface StoredConsent extends ConsentChoices {
  notice: string;
  visitorId: string;
}

declare global {
  interface Window {
    gtag: (...args: unknown[]) => void;
  }
}

// Must match the notice version the API serves; a new notice means the banner asks again.
export const NOTICE_VERSION = '2026-11';
const COOKIE = 'bookstore_consent';
const MAX_AGE_DAYS = 180;
const ANALYTICS_COOKIES = ['_stub_aid'];

@Injectable({ providedIn: 'root' })
export class ConsentService {
  private readonly http = inject(HttpClient);
  private readonly stored = signal<StoredConsent | null>(readCookie());

  readonly decided = computed(() => this.stored()?.notice === NOTICE_VERSION);
  readonly choices = computed<ConsentChoices>(() => {
    const s = this.stored();
    return this.decided() && s ? { analytics: s.analytics, marketing: s.marketing } : { analytics: false, marketing: false };
  });

  async save(choices: ConsentChoices): Promise<void> {
    const visitorId = this.stored()?.visitorId ?? crypto.randomUUID();
    const value: StoredConsent = { ...choices, notice: NOTICE_VERSION, visitorId };
    writeCookie(value);
    this.stored.set(value);
    updateGoogleConsent(choices);
    if (!choices.analytics) {
      // Withdrawal also removes what analytics already stored.
      ANALYTICS_COOKIES.forEach(name => (document.cookie = `${name}=; path=/; max-age=0`));
    }
    await firstValueFrom(this.http.post('/api/consents', { visitorId, noticeVersion: NOTICE_VERSION, choices }));
  }
}

function updateGoogleConsent(c: ConsentChoices): void {
  const state = (granted: boolean) => (granted ? 'granted' : 'denied');
  window.gtag('consent', 'update', {
    analytics_storage: state(c.analytics),
    ad_storage: state(c.marketing),
    ad_user_data: state(c.marketing),
    ad_personalization: state(c.marketing),
  });
}

function readCookie(): StoredConsent | null {
  const raw = document.cookie.split('; ').find(c => c.startsWith(COOKIE + '='))?.slice(COOKIE.length + 1);
  const [notice, a, m, visitorId] = raw ? decodeURIComponent(raw).split('|') : [];
  return visitorId ? { notice, analytics: a === '1', marketing: m === '1', visitorId } : null;
}

function writeCookie(v: StoredConsent): void {
  const value = encodeURIComponent([v.notice, v.analytics ? 1 : 0, v.marketing ? 1 : 0, v.visitorId].join('|'));
  const secure = location.protocol === 'https:' ? '; Secure' : '';
  document.cookie = `${COOKIE}=${value}; path=/; max-age=${MAX_AGE_DAYS * 86400}; SameSite=Lax${secure}`;
}
