import { httpResource } from '@angular/common/http';
import { Component } from '@angular/core';
import { MatButton } from '@angular/material/button';
import { MatCard, MatCardActions, MatCardContent, MatCardHeader, MatCardTitle } from '@angular/material/card';
import { MatProgressBar } from '@angular/material/progress-bar';
import { RouterLink } from '@angular/router';
import { BookSummary, CategoryView, PagedResult } from '../core/api-models';

@Component({
  selector: 'app-dashboard-page',
  imports: [MatCard, MatCardHeader, MatCardTitle, MatCardContent, MatCardActions, MatButton, MatProgressBar, RouterLink],
  templateUrl: './dashboard-page.html',
  // System token: the type scale comes from the theme
  styles: 'h1 { font: var(--mat-sys-headline-small); }',
})
export class DashboardPage {
  // pageSize 1: only the total is needed
  protected readonly books = httpResource<PagedResult<BookSummary>>(() => ({ url: '/api/books', params: { pageSize: 1 } }));
  protected readonly categories = httpResource<CategoryView[]>(() => '/api/categories', { defaultValue: [] });
}
