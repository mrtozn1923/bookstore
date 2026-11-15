import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import axe from 'axe-core';
import { BookDetails } from '../core/api-models';
import { BookDetailPage } from './book-detail-page';

const dune: BookDetails = {
  isbn: '9780441172719', title: 'Dune', author: 'Frank Herbert', category: 'Science Fiction',
  price: 489, publishedYear: 1965, description: 'A desert planet.', available: 25,
};

describe('BookDetailPage accessibility', () => {
  it('has no axe violations once the book has loaded', async () => {
    TestBed.configureTestingModule({ providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()] });
    const fixture = TestBed.createComponent(BookDetailPage);
    fixture.componentRef.setInput('isbn', dune.isbn);
    // Render once so httpResource sends its request; whenStable() would wait for the response
    fixture.detectChanges();
    TestBed.inject(HttpTestingController).expectOne(`/api/books/${dune.isbn}`).flush(dune);
    await fixture.whenStable();

    // jsdom has no layout: rules that need rendering (color-contrast, target-size) belong in the browser tests
    const results = await axe.run(fixture.nativeElement, {
      runOnly: ['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa'],
      rules: { 'color-contrast': { enabled: false } },
    });
    expect(results.violations.map(v => `${v.id}: ${v.nodes.map(n => n.target.join(' ')).join(', ')}`)).toEqual([]);
    expect(results.passes.length).toBeGreaterThan(0);
  });
});
