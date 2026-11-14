import { ComponentFixture, TestBed } from '@angular/core/testing';

import { BookItemComponent } from './book-item.component';
import { TruncatePipe } from '../truncate.pipe';

describe('BookItemComponent', () => {
  let component: BookItemComponent;
  let fixture: ComponentFixture<BookItemComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ BookItemComponent, TruncatePipe ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(BookItemComponent);
    component = fixture.componentInstance;
    component.book = { isbn: '9780441172719', title: 'Dune', price: 450, description: 'A desert planet.', stock: 12 };
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
