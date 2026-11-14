import { computed, inject, Service, signal } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { filter } from 'rxjs';

// Chromium-only APIs that are not in TypeScript 6.0's lib.dom.d.ts
interface BeforeInstallPromptEvent extends Event {
  readonly platforms: string[];
  readonly userChoice: Promise<{ outcome: 'accepted' | 'dismissed'; platform: string }>;
  prompt(): Promise<void>;
}

declare global {
  interface WindowEventMap {
    beforeinstallprompt: BeforeInstallPromptEvent;
  }
  interface Navigator {
    install?(): Promise<unknown>; // Web Install API
    standalone?: boolean; // Safari (WebKit) only
  }
}

const DISMISSED_KEY = 'install-offer-dismissed-at';
const QUIET_DAYS = 30;

@Service()
export class InstallPrompt {
  private deferred: BeforeInstallPromptEvent | null = null;
  private readonly promptable = signal(false);
  private readonly pagesSeen = signal(0);
  private readonly dismissedAt = signal(Number(localStorage.getItem(DISMISSED_KEY) ?? 0));
  private readonly standalone = matchMedia('(display-mode: standalone)');

  readonly installed = signal(this.standalone.matches || navigator.standalone === true);
  // Safari never fires beforeinstallprompt; the user installs from the Share or File menu
  readonly safariHint = 'standalone' in navigator && !this.installed();
  // offer the install only after the user has looked around, and not again for 30 days after "Not now"
  readonly offer = computed(
    () =>
      this.promptable() &&
      !this.installed() &&
      this.pagesSeen() >= 3 &&
      Date.now() - this.dismissedAt() > QUIET_DAYS * 24 * 60 * 60 * 1000,
  );

  constructor() {
    addEventListener('beforeinstallprompt', (event) => {
      event.preventDefault(); // no mini-infobar: we decide when to offer
      this.deferred = event;
      this.promptable.set(true);
    });
    addEventListener('appinstalled', () => {
      this.deferred = null;
      this.promptable.set(false);
      this.installed.set(true);
    });
    this.standalone.addEventListener('change', (e) => this.installed.set(e.matches));
    inject(Router)
      .events.pipe(filter((e) => e instanceof NavigationEnd))
      .subscribe(() => this.pagesSeen.update((n) => n + 1));
  }

  async install(): Promise<string> {
    const event = this.deferred;
    if (event) {
      // prompt() works once per event; Chromium fires a new event later
      this.deferred = null;
      this.promptable.set(false);
      await event.prompt();
      const { outcome } = await event.userChoice;
      if (outcome === 'dismissed') this.notNow();
      return outcome;
    }
    if (navigator.install) {
      try {
        await navigator.install();
        return 'accepted';
      } catch (err) {
        return (err as DOMException).name; // AbortError: the user cancelled, DataError: manifest without id
      }
    }
    return 'unavailable';
  }

  notNow() {
    const now = Date.now();
    localStorage.setItem(DISMISSED_KEY, String(now));
    this.dismissedAt.set(now);
  }
}
