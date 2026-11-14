import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

@Component({
  selector: 'app-book-reviews',
  templateUrl: './book-reviews.component.html',
  styleUrls: ['./book-reviews.component.css']
})
export class BookReviewsComponent implements OnInit {
  isbn = '';

  constructor(private route: ActivatedRoute) { }

  ngOnInit(): void {
    this.route.parent?.paramMap.subscribe(params => {
      this.isbn = params.get('isbn') ?? '';
    });
  }
}
