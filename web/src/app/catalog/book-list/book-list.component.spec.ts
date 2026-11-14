import { ComponentFixture, TestBed } from '@angular/core/testing';

import { BookListComponent } from './book-list.component';
import { BookItemComponent } from '../book-item/book-item.component';
import { InStockPipe } from '../in-stock.pipe';
import { TruncatePipe } from '../truncate.pipe';
import { APP_CONFIG } from '../../app-config';

describe('BookListComponent', () => {
  let component: BookListComponent;
  let fixture: ComponentFixture<BookListComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ BookListComponent, BookItemComponent, InStockPipe, TruncatePipe ],
      providers: [{ provide: APP_CONFIG, useValue: { appName: 'Bookstore', pageSize: 20 } }]
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
