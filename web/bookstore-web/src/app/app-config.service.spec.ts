import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AppConfigService } from './app-config.service';

describe('AppConfigService', () => {
  let service: AppConfigService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(AppConfigService);
    http = TestBed.inject(HttpTestingController);
  });

  it('loads config.json', async () => {
    const loading = service.load();
    http.expectOne('/config.json').flush({
      environment: 'Staging',
      apiBaseUrl: 'https://api.staging.test',
      freeShippingThreshold: 750,
    });
    await loading;
    expect(service.value.apiBaseUrl).toBe('https://api.staging.test');
  });

  it('rejects a config without apiBaseUrl', async () => {
    const loading = service.load();
    http.expectOne('/config.json').flush({ environment: 'Staging', freeShippingThreshold: 750 });
    await expect(loading).rejects.toThrow('config.json has no apiBaseUrl.');
  });

  it('throws when read before loading', () => {
    expect(() => service.value).toThrow('App config is not loaded yet.');
  });
});
