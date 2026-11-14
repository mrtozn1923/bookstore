import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import { inject, PLATFORM_ID, Service, Signal, signal } from '@angular/core';

export type ShareResult = 'shared' | 'copied' | 'cancelled' | 'failed';
export type PermissionView = PermissionState | 'unknown';

// Every device API Bookstore uses goes through this service: feature detection, user activation rules and
// fallbacks live in one place, and components only ask "can I?" and "do it".
@Service()
export class DeviceCapabilities {
  private readonly document = inject(DOCUMENT);
  // no navigator, no window during server-side rendering
  private readonly browser = isPlatformBrowser(inject(PLATFORM_ID));

  readonly supports = {
    share: this.browser && 'share' in navigator,
    clipboard: this.browser && navigator.clipboard?.writeText !== undefined,
    clipboardImage:
      this.browser &&
      typeof ClipboardItem !== 'undefined' &&
      'supports' in ClipboardItem &&
      ClipboardItem.supports('image/png'),
    wakeLock: this.browser && 'wakeLock' in navigator,
    fullscreen: this.browser && this.document.fullscreenEnabled === true,
    geolocation: this.browser && 'geolocation' in navigator,
  };

  private sentinel: WakeLockSentinel | null = null;
  private wakeLockWanted = false;
  readonly screenOn = signal(false);
  readonly fullscreen = signal(false);

  constructor() {
    if (!this.browser) return;
    // the browser releases a wake lock when the page is hidden; take it again when the user comes back
    this.document.addEventListener('visibilitychange', () => {
      if (this.wakeLockWanted && this.document.visibilityState === 'visible')
        void this.acquireWakeLock();
    });
    this.document.addEventListener('fullscreenchange', () =>
      this.fullscreen.set(this.document.fullscreenElement !== null),
    );
  }

  // Call these from a click handler: share, clipboard writes and fullscreen need a user activation.

  async share(data: ShareData): Promise<ShareResult> {
    if (this.supports.share && navigator.canShare(data)) {
      try {
        await navigator.share(data);
        return 'shared';
      } catch (err) {
        // AbortError: the user closed the share sheet; that is an answer, not an error
        return (err as DOMException).name === 'AbortError' ? 'cancelled' : 'failed';
      }
    }
    // no share sheet (e.g. Firefox on the desktop) or data it cannot share: copy the link instead
    return (await this.copyText(data.url ?? data.text ?? '')) ? 'copied' : 'failed';
  }

  async copyText(text: string): Promise<boolean> {
    if (!this.supports.clipboard) return false;
    try {
      await navigator.clipboard.writeText(text);
      return true;
    } catch (err) {
      console.log(`clipboard: ${(err as DOMException).name}`);
      return false;
    }
  }

  async copyImage(png: Promise<Blob>): Promise<boolean> {
    if (!this.supports.clipboardImage) return false;
    try {
      // pass the promise, not the blob: write() starts inside the click, the image follows
      await navigator.clipboard.write([new ClipboardItem({ 'image/png': png })]);
      return true;
    } catch (err) {
      console.log(`clipboard: ${(err as DOMException).name}`);
      return false;
    }
  }

  async toggleFullscreen(element: Element): Promise<void> {
    if (this.document.fullscreenElement) await this.document.exitFullscreen();
    else await element.requestFullscreen({ navigationUI: 'hide' });
  }

  // Keeps the screen on while `on` is true. No user activation needed, but the lock only lives while the page is visible.
  async keepScreenOn(on: boolean): Promise<void> {
    this.wakeLockWanted = on;
    if (on) await this.acquireWakeLock();
    else await this.sentinel?.release();
  }

  private async acquireWakeLock() {
    if (!this.supports.wakeLock || this.sentinel || this.document.visibilityState !== 'visible')
      return;
    try {
      const sentinel = await navigator.wakeLock.request('screen');
      sentinel.addEventListener('release', () => {
        this.sentinel = null;
        this.screenOn.set(false);
        console.log('wake lock: released');
      });
      this.sentinel = sentinel;
      this.screenOn.set(true);
      console.log('wake lock: acquired');
    } catch (err) {
      // NotAllowedError: denied by a policy, the permission or a power-saving mode
      console.log(`wake lock: ${(err as DOMException).name}`);
    }
  }

  currentPosition(options: PositionOptions): Promise<GeolocationPosition> {
    return new Promise((resolve, reject) =>
      navigator.geolocation.getCurrentPosition(resolve, reject, options),
    );
  }

  // The state of a permission, kept up to date when the user changes it in the browser's settings.
  permission(name: PermissionName): Signal<PermissionView> {
    const state = signal<PermissionView>('unknown');
    if (this.browser && 'permissions' in navigator) {
      navigator.permissions.query({ name }).then(
        (status) => {
          state.set(status.state);
          status.addEventListener('change', () => state.set(status.state));
        },
        () => state.set('unknown'), // TypeError: this browser does not know the name
      );
    }
    return state.asReadonly();
  }
}
