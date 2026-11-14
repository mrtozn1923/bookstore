import { Component, inject, resource, signal } from '@angular/core';
import { StorageStatusService } from './storage-status-service';
import { ThemeService } from './theme-service';

@Component({
  selector: 'app-settings-page',
  template: `
    <h1>Settings</h1>
    <label>
      Theme
      <select [value]="themes.theme()" (change)="themes.set($any($event.target).value)">
        <option value="light">Light</option>
        <option value="dark">Dark</option>
      </select>
    </label>
    @if (storage.hasValue()) {
      <p class="storage">
        Offline data: {{ storage.value().usageMb }} MB of {{ storage.value().quotaMb }} MB,
        {{
          storage.value().persisted ? 'kept until you delete it' : 'may be deleted by the browser'
        }}
      </p>
      @if (!storage.value().persisted) {
        <button (click)="keep()">Keep my offline library on this device</button>
      }
    }
    <p class="result">{{ result() }}</p>
  `,
})
export class SettingsPage {
  private readonly statuses = inject(StorageStatusService);
  protected readonly themes = inject(ThemeService);
  protected readonly result = signal('');
  protected readonly storage = resource({ loader: () => this.statuses.status() });

  async keep() {
    const granted = await this.statuses.requestPersistence();
    this.result.set(
      granted
        ? 'Your offline library will be kept.'
        : 'The browser did not grant persistent storage.',
    );
    this.storage.reload();
  }
}
