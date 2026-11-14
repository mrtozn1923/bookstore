import { Component, Input, ViewChild, computed, effect, signal } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { Book } from '../book';
import { QuantityPickerComponent } from '../quantity-picker/quantity-picker.component';
import { FormsModule } from '@angular/forms';
import { NgIf, NgOptimizedImage } from '@angular/common';

@Component({
    selector: 'app-book-detail',
    templateUrl: './book-detail.component.html',
    styleUrls: ['./book-detail.component.css'],
    standalone: true,
    imports: [NgIf, NgOptimizedImage, RouterLink, RouterLinkActive, RouterOutlet, FormsModule, QuantityPickerComponent]
})
export class BookDetailComponent {
  @ViewChild(QuantityPickerComponent) picker!: QuantityPickerComponent;
  @Input() isbn = '';
  @Input() book?: Book;

  title = 'Dune';
  author = 'Frank Herbert';
  coverUrl = 'assets/covers/dune.svg';
  inStock = true;
  price = 450;
  inCart = signal(0);
  total = computed(() => this.inCart() * this.price);
  rating = 90;
  coupon = '';
  giftNote = '';
  amount = 1;

  constructor() {
    effect(() => console.log('Copies in cart:', this.inCart()));
  }

  addToCart(): void {
    this.inCart.update(c => c + 1);
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
