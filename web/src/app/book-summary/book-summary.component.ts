import { Component, OnInit } from '@angular/core';
import { BookRatingComponent } from '../book-rating/book-rating.component';

@Component({
    selector: 'app-book-summary',
    templateUrl: './book-summary.component.html',
    styleUrls: ['./book-summary.component.css'],
    imports: [BookRatingComponent]
})
export class BookSummaryComponent implements OnInit {

  constructor() { }

  ngOnInit(): void {
  }

}
