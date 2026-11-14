import { Injectable } from '@angular/core';
import { PreloadingStrategy, Route } from '@angular/router';
import { EMPTY, Observable } from 'rxjs';

// Preloads only the lazy routes marked with data: { preload: true }, after the first navigation;
// the back office (admin) is never downloaded for customers
@Injectable({ providedIn: 'root' })
export class FlaggedPreloading implements PreloadingStrategy {
  preload(route: Route, load: () => Observable<unknown>): Observable<unknown> {
    return route.data?.['preload'] ? load() : EMPTY;
  }
}
