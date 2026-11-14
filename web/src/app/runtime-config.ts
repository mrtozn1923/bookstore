import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

export interface RuntimeConfig {
  apiBaseUrl: string;
  environment: string;
}

@Injectable({ providedIn: 'root' })
export class RuntimeConfigService {
  private readonly http = inject(HttpClient);
  private readonly config = signal<RuntimeConfig | null>(null);

  get value(): RuntimeConfig {
    const config = this.config();
    if (!config) {
      throw new Error('Runtime config is not loaded');
    }
    return config;
  }

  // config.json is written by the container at startup, not by the Angular build
  async load(): Promise<void> {
    this.config.set(await firstValueFrom(this.http.get<RuntimeConfig>('config.json')));
  }
}
