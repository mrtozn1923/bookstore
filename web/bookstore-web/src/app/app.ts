import { Component, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { CurrencyPipe } from '@angular/common';
import { toSignal } from '@angular/core/rxjs-interop';
import { Book } from './book';

@Component({
  selector: 'app-root',
  imports: [CurrencyPipe],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {
  private readonly http = inject(HttpClient);

  // Relative URL: the dev server proxies /api to the API (proxy.conf.js)
  protected readonly books = toSignal(this.http.get<Book[]>('/api/books'), { initialValue: [] });
}
