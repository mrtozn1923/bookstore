import { DOCUMENT, EnvironmentProviders, PLATFORM_ID, inject, makeEnvironmentProviders, provideAppInitializer } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { NavigationEnd, Router } from '@angular/router';
import { filter, take } from 'rxjs';
import { onCLS, onFCP, onINP, onLCP, onTTFB, type MetricWithAttribution } from 'web-vitals/attribution';

export interface RumOptions {
  endpoint: string;
  sampleRate: number;
}

type Attribution = Record<string, string | number | undefined>;

interface RumReport {
  name: MetricWithAttribution['name'];
  value: number;
  rating: MetricWithAttribution['rating'];
  route: string;
  device: 'mobile' | 'desktop';
  navigationType: MetricWithAttribution['navigationType'];
  attribution: Attribution;
}

export function provideRum(options: RumOptions): EnvironmentProviders {
  return makeEnvironmentProviders([
    provideAppInitializer(() => {
      // Only in the browser: during prerendering there is no page to measure
      if (!isPlatformBrowser(inject(PLATFORM_ID))) return;
      // Sample per page view: either every metric of this visit is sent or none
      if (Math.random() < options.sampleRate) {
        startRum(options, inject(Router), inject(DOCUMENT));
      }
    }),
  ]);
}

function startRum(options: RumOptions, router: Router, document: Document): void {
  const queue: Omit<RumReport, 'route'>[] = [];
  const device = /Mobi|Android/i.test(navigator.userAgent) ? 'mobile' : 'desktop';
  // Without soft navigation reporting every metric belongs to the landing page
  let landingRoute = '(unknown)';
  router.events
    .pipe(filter((e) => e instanceof NavigationEnd), take(1))
    .subscribe(() => (landingRoute = routePattern(router)));

  const add = (metric: MetricWithAttribution) =>
    queue.push({
      name: metric.name,
      value: round(metric.value, metric.name === 'CLS' ? 4 : 0),
      rating: metric.rating,
      device,
      navigationType: metric.navigationType,
      attribution: pickAttribution(metric),
    });

  onTTFB(add);
  onFCP(add);
  onLCP(add);
  onINP(add);
  onCLS(add);

  const flush = () => {
    if (queue.length === 0) return;
    const body = JSON.stringify(queue.splice(0).map((r) => ({ ...r, route: landingRoute })));
    navigator.sendBeacon(options.endpoint, new Blob([body], { type: 'application/json' }));
  };
  // "hidden" is the last moment we can rely on (tab switch, app switch, navigation away)
  document.addEventListener('visibilitychange', () => {
    if (document.visibilityState === 'hidden') flush();
  });
}

// '/books/:isbn' instead of '/books/9781000000371': p75 is computed per route, not per URL
function routePattern(router: Router): string {
  const parts: string[] = [];
  let node = router.routerState.snapshot.root.firstChild;
  while (node) {
    if (node.routeConfig?.path) parts.push(node.routeConfig.path);
    node = node.firstChild;
  }
  return '/' + parts.join('/');
}

function pickAttribution(metric: MetricWithAttribution): Attribution {
  switch (metric.name) {
    case 'LCP':
      return {
        target: metric.attribution.target,
        url: metric.attribution.url,
        ttfb: round(metric.attribution.timeToFirstByte),
        loadDelay: round(metric.attribution.resourceLoadDelay),
        loadDuration: round(metric.attribution.resourceLoadDuration),
        renderDelay: round(metric.attribution.elementRenderDelay),
      };
    case 'INP':
      return {
        target: metric.attribution.interactionTarget,
        type: metric.attribution.interactionType,
        inputDelay: round(metric.attribution.inputDelay),
        processing: round(metric.attribution.processingDuration),
        presentation: round(metric.attribution.presentationDelay),
        script: metric.attribution.longestScript?.entry.invoker,
      };
    case 'CLS':
      return {
        target: metric.attribution.largestShiftTarget,
        largestShift: round(metric.attribution.largestShiftValue ?? 0, 4),
        // When it happened: loadState 'complete' means after the load event (scroll, late content)
        time: round(metric.attribution.largestShiftTime ?? 0),
        loadState: metric.attribution.loadState,
      };
    default:
      return {};
  }
}

function round(value: number, digits = 0): number {
  const f = 10 ** digits;
  return Math.round(value * f) / f;
}
