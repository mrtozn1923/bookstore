import { Component, OnInit, ViewChild } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { Observable } from 'rxjs';
import { switchMap } from 'rxjs/operators';
import { Book } from '../book';
import { BookService } from '../book.service';
import { QuantityPickerComponent } from '../quantity-picker/quantity-picker.component';

@Component({
  selector: 'app-book-detail',
  templateUrl: './book-detail.component.html',
  styleUrls: ['./book-detail.component.css']
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
  book$!: Observable<Book | undefined>;

  constructor(private route: ActivatedRoute, private bookService: BookService) { }

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      this.isbn = params.get('isbn') ?? '';
    });

    this.book$ = this.route.paramMap.pipe(
      switchMap(params => this.bookService.getBook(params.get('isbn') ?? ''))
    );
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
