import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { StoreLayout } from './store-layout';

describe('StoreLayout', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [StoreLayout],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
  });

  it('shows the brand and an empty cart', async () => {
    const fixture = TestBed.createComponent(StoreLayout);
    await fixture.whenStable();
    const element = fixture.nativeElement as HTMLElement;
    expect(element.querySelector('.brand')?.textContent).toContain('Bookstore');
    expect(element.querySelector('[data-testid="cart-link"]')?.textContent).toContain('Cart (0)');
  });
});
