import { Service, signal } from '@angular/core';
import { splitLines } from './split-lines';

export interface ReportSummary {
  rows: number;
  total: number;
}

@Service()
export class OrderReportService {
  readonly rowsRead = signal(0);

  // plain fetch instead of HttpClient: HttpClient would keep the whole body in memory
  async sum(abortSignal: AbortSignal): Promise<ReportSummary> {
    const signal = AbortSignal.any([abortSignal, AbortSignal.timeout(30_000)]);
    signal.addEventListener('abort', () => console.log(`report aborted: ${signal.reason.name}`), {
      once: true,
    });
    const response = await fetch('/api/reports/orders.csv?rows=20000', { signal });
    if (!response.ok || !response.body) throw new Error(`HTTP ${response.status}`);

    let rows = 0;
    let total = 0;
    this.rowsRead.set(0);
    const lines = response.body.pipeThrough(new TextDecoderStream()).pipeThrough(splitLines());
    for await (const line of lines) {
      if (line.startsWith('orderId')) continue;
      total += Number(line.split(',')[4]);
      if (++rows % 5000 === 0) this.rowsRead.set(rows);
    }
    this.rowsRead.set(rows);
    return { rows, total };
  }
}
