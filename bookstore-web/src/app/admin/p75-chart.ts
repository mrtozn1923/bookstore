import { Component, ElementRef, effect, input, viewChild } from '@angular/core';
import { BarController, BarElement, CategoryScale, Chart, Legend, LinearScale, Tooltip } from 'chart.js';
import { RumRow } from './rum-row';

// 'chart.js/auto' registers every chart type, scale and plugin; register only what this bar chart uses
Chart.register(BarController, BarElement, CategoryScale, LinearScale, Tooltip, Legend);

@Component({
  selector: 'app-p75-chart',
  template: `<canvas #canvas></canvas>`,
  styles: `:host { display: block; height: 320px; }`,
})
export class P75Chart {
  readonly rows = input.required<RumRow[]>();
  private readonly canvas = viewChild.required<ElementRef<HTMLCanvasElement>>('canvas');
  private chart?: Chart;

  constructor() {
    effect(() => {
      const lcp = this.rows().filter((r) => r.metric === 'LCP');
      this.chart?.destroy();
      this.chart = new Chart(this.canvas().nativeElement, {
        type: 'bar',
        data: {
          labels: lcp.map((r) => `${r.route} (${r.device})`),
          datasets: [{ label: 'LCP p75 (ms)', data: lcp.map((r) => r.p75) }],
        },
        options: { maintainAspectRatio: false, animation: false },
      });
    });
  }
}
