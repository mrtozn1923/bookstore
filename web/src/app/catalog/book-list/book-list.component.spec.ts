import { ComponentFixture, TestBed } from '@angular/core/testing';

import { BookListComponent } from './book-list.component';
import { BookItemComponent } from '../book-item/book-item.component';
import { InStockPipe } from '../in-stock.pipe';
import { TruncatePipe } from '../truncate.pipe';

describe('BookListComponent', () => {
  let component: BookListComponent;
  let fixture: ComponentFixture<BookListComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ BookListComponent, BookItemComponent, InStockPipe, TruncatePipe ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(BookListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
