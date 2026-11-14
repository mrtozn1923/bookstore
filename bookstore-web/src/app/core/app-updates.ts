import { ApplicationRef, inject, Service, signal } from '@angular/core';
import { SwUpdate } from '@angular/service-worker';

@Service()
export class AppUpdates {
  private readonly updates = inject(SwUpdate);
  readonly ready = signal(false);
  readonly broken = signal('');

  constructor() {
    if (!this.updates.isEnabled) return; // ng serve, or a browser without service workers

    this.updates.versionUpdates.subscribe((event) => {
      if (event.type === 'VERSION_DETECTED') {
        console.log(`update: VERSION_DETECTED ${event.version.hash.slice(0, 8)}`);
      } else if (event.type === 'VERSION_READY') {
        console.log(
          `update: VERSION_READY ${event.currentVersion.hash.slice(0, 8)} -> ${event.latestVersion.hash.slice(0, 8)}`,
        );
        this.ready.set(true);
      } else if (event.type === 'VERSION_INSTALLATION_FAILED') {
        console.log(`update: VERSION_INSTALLATION_FAILED ${event.error}`);
      } else {
        console.log(`update: ${event.type}`);
      }
    });

    // the version serving this tab lost a file it needs; only a full reload helps
    this.updates.unrecoverable.subscribe((event) => {
      console.log(`unrecoverable: ${event.reason}`);
      this.broken.set(event.reason);
    });

    // besides the check the service worker makes on each navigation, check every 6 hours
    void inject(ApplicationRef)
      .whenStable()
      .then(() => setInterval(() => void this.check(), 6 * 60 * 60 * 1000));
  }

  async check() {
    try {
      console.log(`checkForUpdate: ${await this.updates.checkForUpdate()}`);
    } catch (err) {
      console.log(`checkForUpdate failed: ${err}`);
    }
  }

  reload() {
    document.location.reload();
  }
}
