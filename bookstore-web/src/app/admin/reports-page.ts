import { Component, injectAsync, onIdle } from '@angular/core';
import { httpResource } from '@angular/common/http';
import { RumRow } from './rum-row';
import { P75Chart } from './p75-chart';

@Component({
  selector: 'app-reports-page',
  imports: [P75Chart],
  template: `
    <h1>Performance report</h1>
    <button type="button" (click)="exportCsv()">Export CSV</button>
    <table class="report">
      <thead><tr><th>Route</th><th>Metric</th><th>Device</th><th>Samples</th><th>p75</th><th>Rating</th></tr></thead>
      <tbody>
        @for (r of rows.value() ?? []; track $index) {
          <tr [class.alarm]="r.alarm">
            <td>{{ r.route }}</td><td>{{ r.metric }}</td><td>{{ r.device }}</td>
            <td>{{ r.samples }}</td><td>{{ r.p75 }}</td><td>{{ r.rating }}</td>
          </tr>
        }
      </tbody>
    </table>
    <h2>LCP p75 by route</h2>
    <!-- chart.js is downloaded when the chart area becomes visible, and prefetched when the browser is idle -->
    @defer (on viewport; prefetch on idle) {
      <app-p75-chart [rows]="rows.value() ?? []" />
    } @placeholder {
      <div class="chart-slot"></div>
    }
  `,
  styles: `
    .report { width: 100%; border-collapse: collapse; font-size: 14px; }
    .report td, .report th { text-align: left; padding: 4px 8px; border-bottom: 1px solid #e2e8f0; }
    .alarm { background: #fee2e2; }
    /* Same height as the chart: no layout shift when it replaces the placeholder */
    .chart-slot { height: 320px; }
  `,
})
export class ReportsPage {
  protected readonly rows = httpResource<RumRow[]>(() => '/api/rum/p75?minSamples=1');
  // The exporter's code is downloaded when the browser is idle, the service is created on first use
  private readonly exporter = injectAsync(() => import('./report-exporter').then((m) => m.ReportExporter), { prefetch: onIdle });

  protected async exportCsv(): Promise<void> {
    (await this.exporter()).download(this.rows.value() ?? []);
  }
}
