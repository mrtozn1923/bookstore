import { LOCALE_ID } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { App } from './app';

describe('App', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideRouter([])] });
  });

  it('shows the store name and the main navigation', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();

    const element: HTMLElement = fixture.nativeElement;
    expect(element.querySelector('.brand')?.textContent).toBe('Bookstore');
    expect([...element.querySelectorAll('nav a')].map((link) => link.textContent)).toEqual(['Catalog', 'Cart']);
  });

  it('runs with the locale the app uses', () => {
    expect(TestBed.inject(LOCALE_ID)).toBe('tr');
  });
});
