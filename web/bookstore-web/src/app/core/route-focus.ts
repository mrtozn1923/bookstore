import { Injectable, inject } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { filter } from 'rxjs';

// Remembers that a client-side navigation happened, so the next page heading can take focus.
@Injectable({ providedIn: 'root' })
export class RouteFocus {
  private pending = false;

  constructor() {
    inject(Router).events
      .pipe(filter(event => event instanceof NavigationEnd))
      // id 1 is the first load: the browser already starts at the top of the document
      .subscribe(event => this.pending = event.id > 1);
  }

  claim(): boolean {
    const pending = this.pending;
    this.pending = false;
    return pending;
  }
}
