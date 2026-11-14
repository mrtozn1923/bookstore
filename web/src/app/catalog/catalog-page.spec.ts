import { TestbedHarnessEnvironment } from '@angular/cdk/testing/testbed';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { DeferBlockBehavior, TestBed } from '@angular/core/testing';
import { MatSelectHarness } from '@angular/material/select/testing';
import { render, screen } from '@testing-library/angular';
import { dune, emma, hobbit } from '../../testing/books';
import { CartStore } from '../cart/cart-store';
import { CatalogPage } from './catalog-page';

describe('CatalogPage', () => {
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClientTesting()],
      deferBlockBehavior: DeferBlockBehavior.Manual,
    });
    http = TestBed.inject(HttpTestingController);
  });

  it('shows the books that are in stock', async () => {
    const fixture = TestBed.createComponent(CatalogPage);
    TestBed.tick();
    http.expectOne('/api/books').flush([dune, emma]);
    await fixture.whenStable();

    const titles = [...fixture.nativeElement.querySelectorAll('h3')].map((title) => title.textContent);
    expect(titles).toEqual(['Dune']);
  });

  it('adds a book to the cart from its card', async () => {
    const fixture = TestBed.createComponent(CatalogPage);
    TestBed.tick();
    http.expectOne('/api/books').flush([dune]);
    await fixture.whenStable();

    fixture.nativeElement.querySelector('app-book-card button').click();

    expect(TestBed.inject(CartStore).count()).toBe(1);
  });

  it('shows an error and loads again when "Try again" is clicked', async () => {
    const fixture = TestBed.createComponent(CatalogPage);
    TestBed.tick();
    http.expectOne('/api/books').flush(null, { status: 503, statusText: 'Service Unavailable' });
    await fixture.whenStable();

    expect(fixture.nativeElement.querySelector('[role="alert"]').textContent).toBe('Could not load the catalog.');

    fixture.nativeElement.querySelector('button').click();
    TestBed.tick();
    http.expectOne('/api/books').flush([dune]);
    await fixture.whenStable();

    expect(fixture.nativeElement.querySelector('[role="alert"]')).toBeNull();
    expect(fixture.nativeElement.querySelector('h3').textContent).toBe('Dune');
  });
});

describe('CatalogPage with Testing Library', () => {
  it('shows the books once the catalog arrives', async () => {
    await render(CatalogPage, {
      providers: [provideHttpClientTesting()],
      deferBlockBehavior: DeferBlockBehavior.Manual,
    });
    TestBed.inject(HttpTestingController).expectOne('/api/books').flush([dune, emma]);

    expect(await screen.findByRole('heading', { name: 'Dune' })).toBeInTheDocument();
    expect(screen.queryByRole('heading', { name: 'Emma' })).not.toBeInTheDocument();
  });
});

describe('CatalogPage sorting', () => {
  it('sorts the books by price', async () => {
    const { fixture } = await render(CatalogPage, {
      providers: [provideHttpClientTesting()],
      deferBlockBehavior: DeferBlockBehavior.Manual,
    });
    TestBed.inject(HttpTestingController).expectOne('/api/books').flush([dune, hobbit, { ...emma, stock: 2 }]);
    const loader = TestbedHarnessEnvironment.loader(fixture);

    const sort = await loader.getHarness(MatSelectHarness.with({ label: 'Sort by' }));
    await sort.clickOptions({ text: 'Price: low to high' });

    expect(await sort.getValueText()).toBe('Price: low to high');
    const titles = screen.getAllByRole('heading', { level: 3 }).map((heading) => heading.textContent);
    expect(titles).toEqual(['Emma', 'The Hobbit', 'Dune']);
  });
});
