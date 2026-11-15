import { ComponentFixture, TestBed } from '@angular/core/testing';
import { BookCard } from './book-card';

describe('BookCard', () => {
  let fixture: ComponentFixture<BookCard>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [BookCard] }).compileComponents();
    fixture = TestBed.createComponent(BookCard);
    fixture.componentRef.setInput('book', {
      isbn: '9780141439518',
      title: 'Emma',
      author: 'Jane Austen',
      price: 189,
    });
    await fixture.whenStable();
  });

  it('shows the title', () => {
    const title = (fixture.nativeElement as HTMLElement).querySelector('h3');
    expect(title?.textContent).toContain('Emma');
  });
});
