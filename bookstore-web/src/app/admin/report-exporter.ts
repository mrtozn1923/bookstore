import { Injectable } from '@angular/core';
import { RumRow } from './rum-row';

// Needed only when someone clicks "Export CSV": loaded with injectAsync, not part of any initial chunk
@Injectable({ providedIn: 'root' })
export class ReportExporter {
  download(rows: RumRow[]): void {
    const header = 'route,metric,device,samples,p75,rating';
    const lines = rows.map((r) => [r.route, r.metric, r.device, r.samples, r.p75, r.rating].join(','));
    const url = URL.createObjectURL(new Blob([[header, ...lines].join('\n')], { type: 'text/csv' }));
    const link = Object.assign(document.createElement('a'), { href: url, download: 'rum-p75.csv' });
    link.click();
    URL.revokeObjectURL(url);
  }
}
