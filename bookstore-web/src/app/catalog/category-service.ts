import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { shareReplay } from 'rxjs';
import { apiUrl } from '../api';

@Service()
export class CategoryService {
  // one request for the whole app; every later subscriber gets the cached list
  readonly categories$ = inject(HttpClient)
    .get<string[]>(`${apiUrl}/categories`)
    .pipe(shareReplay({ bufferSize: 1, refCount: false }));
}
