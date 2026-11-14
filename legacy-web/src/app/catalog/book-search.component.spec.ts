import type { Mock } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { of } from 'rxjs';
import { BookSearchComponent } from './book-search.component';
import { BookService } from './book.service';

describe('BookSearchComponent', () => {
  beforeEach(() => {
    vi.useFakeTimers({ advanceTimeDelta: 1, shouldAdvanceTime: true });
  });
  afterEach(() => {
    vi.useRealTimers();
  });
  let fixture: ComponentFixture<BookSearchComponent>;
  let bookService: { search: Mock<BookService['search']> };

  beforeEach(async () => {
    bookService = {
      search: vi.fn<BookService['search']>().mockName('BookService.search'),
    };
    bookService.search.mockReturnValue(of([{ isbn: '9780441013593', title: 'Dune', price: 450 }]));

    await TestBed.configureTestingModule({
      declarations: [BookSearchComponent],
      imports: [ReactiveFormsModule],
      providers: [{ provide: BookService, useValue: bookService }],
    }).compileComponents();

    fixture = TestBed.createComponent(BookSearchComponent);
    fixture.detectChanges();
  });

  it('should search after the user stops typing', async () => {
    const input = fixture.nativeElement.querySelector('input') as HTMLInputElement;
    input.value = 'dune';
    input.dispatchEvent(new Event('input'));

    await vi.advanceTimersByTimeAsync(299);
    expect(bookService.search).not.toHaveBeenCalled();

    await vi.advanceTimersByTimeAsync(1);
    fixture.detectChanges();
    expect(bookService.search).toHaveBeenCalledWith('dune');
    expect(fixture.nativeElement.querySelector('li').textContent).toBe('Dune');
  });

  it('should not search for a single letter', async () => {
    fixture.componentInstance.term.setValue('d');
    await vi.advanceTimersByTimeAsync(300);

    expect(bookService.search).not.toHaveBeenCalled();
  });
});
