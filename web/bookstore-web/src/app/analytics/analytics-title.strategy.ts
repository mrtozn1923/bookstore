import { Injectable, inject } from '@angular/core';
import { DefaultTitleStrategy, RouterStateSnapshot } from '@angular/router';
import { AnalyticsService } from './analytics.service';

// NavigationEnd fires before the router updates document.title;
// sending page_view here gives every view its own title.
@Injectable()
export class AnalyticsTitleStrategy extends DefaultTitleStrategy {
  private readonly analytics = inject(AnalyticsService);

  override updateTitle(snapshot: RouterStateSnapshot): void {
    super.updateTitle(snapshot);
    this.analytics.pageView();
  }
}
