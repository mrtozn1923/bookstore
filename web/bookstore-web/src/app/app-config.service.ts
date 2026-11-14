import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

// Bu dosyaya giren her değer tarayıcıda herkese açıktır: secret yok.
export interface AppConfig {
  environment: string;
  apiBaseUrl: string;
  freeShippingThreshold: number;
}

@Injectable({ providedIn: 'root' })
export class AppConfigService {
  private readonly http = inject(HttpClient);
  private readonly config = signal<AppConfig | null>(null);

  get value(): AppConfig {
    const config = this.config();
    if (!config) {
      throw new Error('App config is not loaded yet.');
    }
    return config;
  }

  async load(): Promise<void> {
    const config = await firstValueFrom(this.http.get<AppConfig>('/config.json'));
    if (!config.apiBaseUrl) {
      throw new Error('config.json has no apiBaseUrl.');
    }
    this.config.set(config);
  }
}
