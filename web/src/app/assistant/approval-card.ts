import { Component, DestroyRef, computed, inject, input, output, signal } from '@angular/core';

export interface ApprovalRequest {
  approvalId: string;
  tool: string;
  arguments: Record<string, unknown> | null;
  expiresAt: number;
  state: 'pending' | 'approved' | 'rejected';
}

// Hassas tool'lar için insan diliyle bir özet; bilinmeyen tool'un adı ve argümanları olduğu gibi gösterilir
export function describe(tool: string, args: Record<string, unknown> | null): string {
  if (tool === 'cancel_order') {
    return `Cancel order ${args?.['orderId']}?`;
  }
  return `Run ${tool} with ${JSON.stringify(args)}?`;
}

@Component({
  selector: 'app-approval-card',
  template: `
    <div class="card">
      <p>{{ question() }}</p>
      @if (request().state !== 'pending') {
        <p class="muted">{{ request().state === 'approved' ? 'Approved.' : 'Rejected.' }}</p>
      } @else if (secondsLeft() === 0) {
        <p class="muted">This request expired. Ask the assistant again.</p>
      } @else {
        <button type="button" (click)="decide.emit(true)" [disabled]="disabled()">Approve</button>
        <button type="button" (click)="decide.emit(false)" [disabled]="disabled()">Reject</button>
        <span class="muted">{{ secondsLeft() }} s left</span>
      }
    </div>
  `,
  styles: `.card { padding: 8px 12px; border: 1px solid #f59e0b; border-radius: 8px; background: #fffbeb; }
    .muted { color: #6b7280; font-size: 0.875rem; }`,
})
export class ApprovalCard {
  readonly request = input.required<ApprovalRequest>();
  readonly disabled = input(false);
  readonly decide = output<boolean>();
  protected readonly question = computed(() => describe(this.request().tool, this.request().arguments));

  private readonly now = signal(Date.now());
  protected readonly secondsLeft = computed(() =>
    Math.max(0, Math.ceil((this.request().expiresAt - this.now()) / 1000)),
  );

  constructor() {
    // Geri sayım için saniyede bir "şimdi"yi güncelle
    const timer = setInterval(() => this.now.set(Date.now()), 1000);
    inject(DestroyRef).onDestroy(() => clearInterval(timer));
  }
}
