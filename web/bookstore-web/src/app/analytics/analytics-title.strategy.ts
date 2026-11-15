import { Injectable, inject } from '@angular/core';
import { DefaultTitleStrategy, RouterStateSnapshot } from '@angular/router';
import { RumService } from '../rum/rum.service';
import { AnalyticsService } from './analytics.service';

// NavigationEnd fires before the router updates document.title;
// sending page_view here gives every view its own title.
@Injectable()
export class AnalyticsTitleStrategy extends DefaultTitleStrategy {
  private readonly analytics = inject(AnalyticsService);
  private readonly rum = inject(RumService);

  override updateTitle(snapshot: RouterStateSnapshot): void {
    super.updateTitle(snapshot);
    this.analytics.pageView();
    this.rum.landedOn(routePattern(snapshot));
  }
}

// "/blog/:slug" instead of "/blog/dune": one group per route, as in the Search Console report
function routePattern(snapshot: RouterStateSnapshot): string {
  let route = snapshot.root;
  const parts: string[] = [];
  while (route.firstChild) {
    route = route.firstChild;
    if (route.routeConfig?.path) {
      parts.push(route.routeConfig.path);
    }
  }
  return '/' + parts.join('/');
}
