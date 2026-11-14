import { Component, OnInit } from '@angular/core';

@Component({
  selector: 'app-book-detail',
  templateUrl: './book-detail.component.html',
  styleUrls: ['./book-detail.component.css']
})
export class BookDetailComponent {
  title = 'Dune';
  author = 'Frank Herbert';
  coverUrl = 'assets/covers/dune.svg';
  inStock = true;
  inCart = 0;
  rating = 90;
  coupon = '';
  giftNote = '';

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
}
