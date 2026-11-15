import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { provideRouter } from '@angular/router';
import { BookSummary } from '../core/api-models';
import { ReadingListPage } from './reading-list-page';

const book = (isbn: string, title: string): BookSummary =>
  ({ isbn, title, author: 'Frank Herbert', category: 'Science Fiction', price: 489, publishedYear: 1965 });
const books = [book('1', 'Dune'), book('2', 'Emma'), book('3', 'Middlemarch')];

async function render() {
  TestBed.configureTestingModule({
    imports: [ReadingListPage],
    providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
  });
  const fixture = TestBed.createComponent(ReadingListPage);
  const http = TestBed.inject(HttpTestingController);
  http.expectOne('/api/reading-list').flush(books);
  await fixture.whenStable();
  const element = fixture.nativeElement as HTMLElement;
  const titles = () => [...element.querySelectorAll('.title a')].map(a => a.textContent);
  return { fixture, http, element, titles };
}

describe('ReadingListPage', () => {
  it('moves a book with the arrow button, saves the new order and keeps focus on the moved book', async () => {
    const { fixture, http, element, titles } = await render();
    element.querySelector<HTMLButtonElement>('button[aria-label="Move Middlemarch up"]')!.click();
    await fixture.whenStable();
    expect(titles()).toEqual(['Dune', 'Middlemarch', 'Emma']);
    expect(http.expectOne('/api/reading-list/order').request.body).toEqual({ isbns: ['1', '3', '2'] });
    expect(document.activeElement?.getAttribute('aria-label')).toBe('Move Middlemarch up');
    expect(element.querySelector('[aria-live]')?.textContent).toBe('Middlemarch moved to position 2 of 3');
  });

  it('applies a drop from the drop list', async () => {
    const { fixture, http, titles } = await render();
    fixture.debugElement.query(By.css('[cdkDropList]')).triggerEventHandler('cdkDropListDropped', { previousIndex: 0, currentIndex: 2 });
    await fixture.whenStable();
    expect(titles()).toEqual(['Emma', 'Middlemarch', 'Dune']);
    http.expectOne('/api/reading-list/order').flush(null, { status: 204, statusText: 'No Content' });
  });

  it('shows the old order again when the API refuses the new one', async () => {
    const { fixture, http, element, titles } = await render();
    element.querySelector<HTMLButtonElement>('button[aria-label="Move Dune down"]')!.click();
    http.expectOne('/api/reading-list/order').flush({ title: 'Bad Request' }, { status: 400, statusText: 'Bad Request' });
    await fixture.whenStable();
    expect(titles()).toEqual(['Dune', 'Emma', 'Middlemarch']);
    expect(element.querySelector('[aria-live]')?.textContent).toBe('The new order could not be saved');
  });
});
