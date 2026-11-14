import { Component, ViewChild } from '@angular/core';
import { QuantityPickerComponent } from '../quantity-picker/quantity-picker.component';

@Component({
  selector: 'app-book-detail',
  templateUrl: './book-detail.component.html',
  styleUrls: ['./book-detail.component.css']
})
export class BookDetailComponent {
  @ViewChild(QuantityPickerComponent) picker!: QuantityPickerComponent;

  title = 'Dune';
  author = 'Frank Herbert';
  coverUrl = 'assets/covers/dune.svg';
  inStock = true;
  inCart = 0;
  rating = 90;
  coupon = '';
  giftNote = '';
  amount = 1;

  addToCart(): void {
    this.inCart++;
  }

  onGiftNoteInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.giftNote = input.value;
  }

  applyCoupon(code: string): void {
    this.coupon = code;
  }

  resetQuantity(): void {
    this.picker.reset();
  }
}
