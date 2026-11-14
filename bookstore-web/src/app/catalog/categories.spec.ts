import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ApplicationRef } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Categories } from './categories';

describe('Categories (signal service with httpResource)', () => {
  it('loads the list once and exposes it as a signal', async () => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    const categories = TestBed.inject(Categories);
    const http = TestBed.inject(HttpTestingController);
    expect(categories.list.value()).toEqual([]);

    // the resource sends its request when its params are computed, which happens in an effect
    TestBed.tick();
    http.expectOne('http://localhost:5280/api/categories').flush([{ slug: 'classics', name: 'Classics' }]);
    await TestBed.inject(ApplicationRef).whenStable();

    expect(categories.list.status()).toBe('resolved');
    expect(categories.list.value()).toEqual([{ slug: 'classics', name: 'Classics' }]);
  });
});
