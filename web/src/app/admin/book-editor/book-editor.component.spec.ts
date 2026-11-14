import { ComponentFixture, TestBed } from '@angular/core/testing';

import { BookEditorComponent } from './book-editor.component';

describe('BookEditorComponent', () => {
  let component: BookEditorComponent;
  let fixture: ComponentFixture<BookEditorComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [BookEditorComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(BookEditorComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('book', { isbn: '9780441172719', title: 'Dune', price: 450, stock: 12, description: '' });
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
