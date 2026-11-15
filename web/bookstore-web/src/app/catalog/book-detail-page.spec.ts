import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { BookDetails } from '../core/api-models';
import { BookDetailPage } from './book-detail-page';

describe('BookDetailPage ISBN', () => {
  const dune: BookDetails = {
    isbn: '9780441172719',
    isbn10: '0441172717',
    title: 'Dune',
    author: 'Frank Herbert',
    category: 'Science Fiction',
    price: 489,
    publishedYear: 1965,
    description: 'A desert planet.',
    available: 3,
  };

  async function render(book: BookDetails) {
    TestBed.configureTestingModule({
      imports: [BookDetailPage],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    });
    const fixture = TestBed.createComponent(BookDetailPage);
    fixture.componentRef.setInput('isbn', book.isbn);
    fixture.detectChanges();
    TestBed.inject(HttpTestingController).expectOne(`/api/books/${book.isbn}`).flush(book);
    await fixture.whenStable();
    return (fixture.nativeElement.querySelector('[data-testid="isbn"]') as HTMLElement).textContent;
  }

  it('shows the ISBN-10 next to the ISBN-13', async () => {
    const text = await render(dune);

    expect(text).toContain('ISBN 9780441172719');
    expect(text).toContain('ISBN-10 0441172717');
  });

  it('hides the ISBN-10 when the book has none', async () => {
    const text = await render({ ...dune, isbn: '9791032305690', isbn10: null });

    expect(text).not.toContain('ISBN-10');
  });
});
