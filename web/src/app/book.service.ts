import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';
import { delay } from 'rxjs/operators';

@Injectable({ providedIn: 'root' })
export class BookService {
  private titles = ['Dune', 'Emma', 'The Hobbit', 'Middlemarch', 'Frankenstein', 'Pride and Prejudice'];

  search(term: string): Observable<string[]> {
    const query = term.toLowerCase();
    const matches = this.titles.filter(t => t.toLowerCase().includes(query));
    return of(matches).pipe(delay(300));
  }
}
