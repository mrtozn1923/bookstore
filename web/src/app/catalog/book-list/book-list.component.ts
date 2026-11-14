import { Component, Inject, OnInit } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AppConfig, APP_CONFIG } from '../../app-config';
import { Book } from '../../book';
import { CartService } from '../../cart.service';
import { LoggerService } from '../../logger.service';
import { InStockPipe } from '../in-stock.pipe';
import { BookItemComponent } from '../book-item/book-item.component';
import { BookSearchComponent } from '../../book-search/book-search.component';

@Component({
    selector: 'app-book-list',
    templateUrl: './book-list.component.html',
    styleUrls: ['./book-list.component.css'],
    standalone: true,
    imports: [RouterLink, BookItemComponent, InStockPipe, BookSearchComponent]
})
export class BookListComponent implements OnInit {
  books: Book[] = [
    {
      isbn: '9780441172719', title: 'Dune', price: 450, stock: 12,
      description: 'A desert planet, a noble family and a war over the spice melange.'
    },
    {
      isbn: '9780141439587', title: 'Emma', price: 320, stock: 8,
      description: 'A young matchmaker in an English village keeps getting love wrong.'
    },
    {
      isbn: '9780141439471', title: 'Frankenstein', price: 280, stock: 0,
      description: 'A young scientist gives life to a creature and then runs from it.'
    }
  ];
  sort = 'title';
  page = 1;

  constructor(
    private cartService: CartService,
    private logger: LoggerService,
    @Inject(APP_CONFIG) private config: AppConfig,
    private route: ActivatedRoute,
    private router: Router) { }

  ngOnInit(): void {
    this.route.queryParamMap.subscribe(params => {
      this.sort = params.get('sort') ?? 'title';
      this.page = Number(params.get('page') ?? 1);
    });
  }

  openBook(isbn: string): void {
    this.router.navigate(['/books', isbn]);
  }

  goToPage(page: number): void {
    this.router.navigate(['/books'], { queryParams: { page } });
  }

  addToCart(book: Book): void {
    this.cartService.add(book);
    this.logger.log(`${this.config.appName}: ${book.title} added to the cart`);
  }

  addBook(): void {
    const newBook: Book = {
      isbn: '9780547928227', title: 'The Hobbit', price: 390, stock: 5,
      description: 'A comfortable hobbit is pulled into a long journey with thirteen dwarves.'
    };
    this.books = [...this.books, newBook];
  }
}
