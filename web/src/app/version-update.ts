import { Injectable, inject, signal } from '@angular/core';
import { SwUpdate, VersionReadyEvent } from '@angular/service-worker';
import { filter } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class VersionUpdate {
  private readonly swUpdate = inject(SwUpdate);
  readonly updateReady = signal(false);

  constructor() {
    if (!this.swUpdate.isEnabled) {
      return;
    }
    this.swUpdate.versionUpdates
      .pipe(filter((event): event is VersionReadyEvent => event.type === 'VERSION_READY'))
      .subscribe((event) => {
        console.log(`New version ready: ${event.latestVersion.hash}`);
        this.updateReady.set(true);
      });
  }

  async reload(): Promise<void> {
    await this.swUpdate.activateUpdate();
    document.location.reload();
  }
}
