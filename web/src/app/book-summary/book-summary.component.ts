import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { BookRatingComponent } from '../book-rating/book-rating.component';

@Component({
    selector: 'app-book-summary',
    templateUrl: './book-summary.component.html',
    styleUrls: ['./book-summary.component.css'],
    changeDetection: ChangeDetectionStrategy.Eager,
    imports: [BookRatingComponent]
})
export class BookSummaryComponent implements OnInit {

  constructor() { }

  ngOnInit(): void {
  }

}
