import { Component, inject, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AnalyticsService } from '../analytics/analytics.service';

@Component({
  selector: 'app-post-page',
  imports: [RouterLink],
  template: `
    <h1>{{ slug() }}</h1>
    <pre><code>dotnet run --project src/Bookstore.Analytics.Cli</code></pre>
    <button type="button" (click)="copy()">Copy</button>
    <a routerLink="/blog/weekly-report" (click)="next()">Next post</a>
    <a href="/downloads/analytics-ve-olcum.pdf">PDF</a>
  `,
})
export class PostPage {
  readonly slug = input.required<string>();
  private readonly analytics = inject(AnalyticsService);

  copy(): void {
    void navigator.clipboard?.writeText('dotnet run --project src/Bookstore.Analytics.Cli');
    this.analytics.codeCopied('bash');
  }

  next(): void {
    this.analytics.seriesNavigated('analytics', 'next');
  }
}
