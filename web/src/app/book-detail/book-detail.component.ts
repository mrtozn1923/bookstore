import { Component, OnInit, ViewChild } from '@angular/core';
import { ActivatedRoute, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { Book } from '../book';
import { QuantityPickerComponent } from '../quantity-picker/quantity-picker.component';
import { FormsModule } from '@angular/forms';
import { NgIf } from '@angular/common';

@Component({
    selector: 'app-book-detail',
    templateUrl: './book-detail.component.html',
    styleUrls: ['./book-detail.component.css'],
    standalone: true,
    imports: [NgIf, RouterLink, RouterLinkActive, RouterOutlet, FormsModule, QuantityPickerComponent]
})
export class BookDetailComponent implements OnInit {
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
  isbn = '';
  book?: Book;

  constructor(private route: ActivatedRoute) { }

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      this.isbn = params.get('isbn') ?? '';
    });

    this.route.data.subscribe(data => this.book = data.book);
  }

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
