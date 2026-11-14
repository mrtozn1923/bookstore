import { CurrencyPipe } from '@angular/common';
import { Component, effect, inject, resource, signal } from '@angular/core';
import { OrderReportService } from './order-report-service';

@Component({
  selector: 'app-order-report-page',
  imports: [CurrencyPipe],
  template: `
    <h1>Order report</h1>
    <button (click)="run.set(run() + 1)">Sum the report</button>
    @if (report.isLoading()) {
      <p>{{ rowsRead() }} rows read…</p>
    } @else if (report.hasValue()) {
      <p>{{ report.value().rows }} rows, {{ report.value().total | currency: 'TRY' }}</p>
    }
  `,
})
export class OrderReportPage {
  private readonly reports = inject(OrderReportService);
  protected readonly run = signal(0);
  protected readonly rowsRead = this.reports.rowsRead;

  // resource() aborts the previous load when run() changes or the page is destroyed
  protected readonly report = resource({
    params: () => this.run() || undefined,
    loader: ({ abortSignal }) => this.reports.sum(abortSignal),
  });

  constructor() {
    effect(() => console.log(`rows read: ${this.rowsRead()}, status: ${this.report.status()}`));
  }
}
