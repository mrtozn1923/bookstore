import { httpResource } from '@angular/common/http';
import { Component, input } from '@angular/core';

export interface Rating {
  average: number;
  count: number;
}

@Component({
  selector: 'app-rating-summary',
  template: `
    @if (rating.hasValue()) {
      <p class="rating">{{ rating.value().average }} / 5 ({{ rating.value().count }} ratings)</p>
    }
  `,
})
export class RatingSummary {
  readonly isbn = input.required<string>();
  protected readonly rating = httpResource<Rating>(() => `/api/books/${this.isbn()}/rating`);
}
