import { Component, ElementRef, afterNextRender, viewChild } from '@angular/core';

@Component({
    selector: 'app-search-box',
    template: `<input #searchInput placeholder="Search books...">`,
    standalone: true
})
export class SearchBoxComponent {
  searchInput = viewChild.required<ElementRef<HTMLInputElement>>('searchInput');

  constructor() {
    afterNextRender(() => this.searchInput().nativeElement.focus());
  }
}
