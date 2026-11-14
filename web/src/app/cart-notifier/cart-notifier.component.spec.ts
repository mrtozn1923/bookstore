import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CartNotifierComponent } from './cart-notifier.component';

describe('CartNotifierComponent', () => {
  let component: CartNotifierComponent;
  let fixture: ComponentFixture<CartNotifierComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
    imports: [CartNotifierComponent]
})
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(CartNotifierComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
