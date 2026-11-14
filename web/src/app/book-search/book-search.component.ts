import { Component, OnInit } from '@angular/core';
import { Observable, Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged, switchMap } from 'rxjs/operators';
import { BookService } from '../book.service';
import { NgFor, AsyncPipe } from '@angular/common';

@Component({
    selector: 'app-book-search',
    template: `
    <input #box (input)="search(box.value)" placeholder="Search books...">
    <p *ngFor="let title of results$ | async">{{ title }}</p>
  `,
    standalone: true,
    imports: [NgFor, AsyncPipe]
})
export class BookSearchComponent implements OnInit {
  results$!: Observable<string[]>;
  private searchTerms = new Subject<string>();

  constructor(private bookService: BookService) { }

  search(term: string): void {
    this.searchTerms.next(term);
  }

  ngOnInit(): void {
    this.results$ = this.searchTerms.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      switchMap(term => this.bookService.search(term))
    );
  }
}
