import { httpResource } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { DeviceCapabilities } from '../core/device-capabilities';
import { nearest, Warehouse } from './nearest-warehouse';

@Component({
  selector: 'app-delivery-page',
  template: `
    <h1>Delivery address</h1>
    <p>Bulk orders ship from the warehouse nearest to your school, library or office.</p>
    <textarea
      class="address"
      rows="4"
      placeholder="Address"
      [value]="address()"
      (paste)="paste($event)"
      (input)="address.set($any($event.target).value)"
    ></textarea>

    @if (device.supports.geolocation && location() !== 'denied') {
      <button class="locate" (click)="useMyLocation()" [disabled]="locating()">
        Use my location
      </button>
    }
    @if (location() === 'denied') {
      <p class="hint">
        Location is blocked for Bookstore in your browser settings. Type the address instead.
      </p>
    }
    @if (message()) {
      <p class="warehouse">{{ message() }}</p>
    }
  `,
})
export class DeliveryPage {
  protected readonly device = inject(DeviceCapabilities);
  protected readonly location = this.device.permission('geolocation');
  protected readonly address = signal('');
  protected readonly locating = signal(false);
  protected readonly message = signal('');
  private readonly warehouses = httpResource<Warehouse[]>(() => '/api/warehouses');

  // The paste event needs no permission: the user chose to paste. Tidy up addresses copied from e-mails.
  paste(event: ClipboardEvent) {
    const text = event.clipboardData?.getData('text/plain');
    if (!text) return;
    event.preventDefault();
    const tidy = text
      .split(/\r?\n/)
      .map((line) => line.replace(/\s+/g, ' ').trim())
      .filter((line) => line !== '')
      .join('\n');
    this.address.set(tidy);
    console.log(`paste: ${JSON.stringify(text)} -> ${JSON.stringify(tidy)}`);
  }

  async useMyLocation() {
    this.locating.set(true);
    this.message.set('Finding your location...');
    try {
      const { coords } = await this.device.currentPosition({
        enableHighAccuracy: false, // city level is enough; GPS costs battery and time
        timeout: 10_000,
        maximumAge: 5 * 60_000, // a position from the last 5 minutes is fine
      });
      const found = nearest(this.warehouses.value() ?? [], coords);
      console.log(
        `location: accuracy ${Math.round(coords.accuracy)} m, nearest ${found?.warehouse.code}`,
      );
      this.message.set(
        found
          ? `Nearest warehouse: ${found.warehouse.name} (${found.km} km).`
          : 'Warehouses could not be loaded.',
      );
    } catch (err) {
      const error = err as GeolocationPositionError;
      console.log(`location: error ${error.code} ${error.message}`);
      const messages: Record<number, string> = {
        [error.PERMISSION_DENIED]:
          'Without your location we cannot pick a warehouse. Type the address instead.',
        [error.POSITION_UNAVAILABLE]:
          'Your location is not available right now. Type the address instead.',
        [error.TIMEOUT]: 'Finding your location took too long. Try again or type the address.',
      };
      this.message.set(messages[error.code]);
    } finally {
      this.locating.set(false);
    }
  }
}
