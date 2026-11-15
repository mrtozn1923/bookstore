import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { DashboardPage } from './dashboard-page';

describe('DashboardPage', () => {
  it('shows the catalog totals from the API', async () => {
    TestBed.configureTestingModule({
      imports: [DashboardPage],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    });
    const fixture = TestBed.createComponent(DashboardPage);
    const http = TestBed.inject(HttpTestingController);
    // Runs the httpResource requests; whenStable() would wait for the pending requests themselves
    fixture.detectChanges();

    http.expectOne(r => r.url === '/api/books').flush({ items: [], total: 20, page: 1, pageSize: 1 });
    http.expectOne('/api/categories').flush([{ name: 'Classics', slug: 'classics' }, { name: 'Fantasy', slug: 'fantasy' }]);
    await fixture.whenStable();

    const summary = (fixture.nativeElement as HTMLElement).querySelector('[data-testid="catalog-summary"]');
    expect(summary?.textContent).toBe('20 books in 2 categories');
  });
});
