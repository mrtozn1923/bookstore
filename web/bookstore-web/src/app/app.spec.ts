import { TestBed } from '@angular/core/testing';
import { App } from './app';
import { AppConfigService } from './app-config.service';

describe('App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [
        {
          provide: AppConfigService,
          useValue: {
            value: { environment: 'Test', apiBaseUrl: 'https://api.test', freeShippingThreshold: 500 },
          },
        },
      ],
    }).compileComponents();
  });

  it('renders values from the runtime config', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('#env')?.textContent).toContain('Test · API: https://api.test');
  });
});
