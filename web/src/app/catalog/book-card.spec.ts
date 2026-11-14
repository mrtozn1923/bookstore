import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, DeferBlockBehavior, DeferBlockState, TestBed } from '@angular/core/testing';
import { dune, emma } from '../../testing/books';
import { BookCard } from './book-card';

describe('BookCard', () => {
  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClientTesting()],
      deferBlockBehavior: DeferBlockBehavior.Manual,
    });
    const fixture = TestBed.createComponent(BookCard);
    fixture.componentRef.setInput('book', dune);
    await fixture.whenStable();
  });

  function element(): HTMLElement {
    return TestBed.getLastFixture<BookCard>().nativeElement;
  }

  it('shows the title, the author and the price', () => {
    expect(element().querySelector('h3')?.textContent).toBe('Dune');
    expect(element().querySelector('.author')?.textContent).toBe('Frank Herbert');
    expect(element().querySelector('.price')?.textContent).toBe('₺450,00');
  });

  it('emits the book when "Add to cart" is clicked', () => {
    const fixture: ComponentFixture<BookCard> = TestBed.getLastFixture();
    const added = vi.fn();
    fixture.componentInstance.addToCart.subscribe(added);

    element().querySelector('button')!.click();

    expect(added).toHaveBeenCalledExactlyOnceWith(dune);
  });

  it('replaces the button with "Out of stock" when there is no stock', async () => {
    const fixture = TestBed.getLastFixture<BookCard>();
    fixture.componentRef.setInput('book', emma);
    await fixture.whenStable();

    expect(element().querySelector('button')).toBeNull();
    expect(element().querySelector('.stock')?.textContent).toBe('Out of stock');
  });

  it('keeps the ratings deferred until the card is rendered completely', async () => {
    const fixture = TestBed.getLastFixture<BookCard>();
    expect(element().querySelector('.rating-placeholder')?.textContent).toBe('Ratings');

    const [ratings] = await fixture.getDeferBlocks();
    await ratings.render(DeferBlockState.Complete);
    TestBed.tick();
    TestBed.inject(HttpTestingController)
      .expectOne('/api/books/9780441013593/rating')
      .flush({ average: 4.5, count: 120 });
    await fixture.whenStable();

    expect(element().querySelector('.rating')?.textContent).toBe('4.5 / 5 (120 ratings)');
  });
});
