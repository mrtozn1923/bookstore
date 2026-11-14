import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { BookRatingComponent } from '../book-rating/book-rating.component';

@Component({
    selector: 'app-book-reviews',
    templateUrl: './book-reviews.component.html',
    styleUrls: ['./book-reviews.component.css'],
    standalone: true,
    imports: [BookRatingComponent]
})
export class BookReviewsComponent implements OnInit {
  isbn = '';
  score = 0;

  constructor(private route: ActivatedRoute) { }

  ngOnInit(): void {
    this.route.parent?.paramMap.subscribe(params => {
      this.isbn = params.get('isbn') ?? '';
    });
  }

  saveScore(stars: number): void {
    console.log(`Rated ${this.isbn}: ${stars} stars`);
  }
}
