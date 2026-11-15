import { Service, signal } from '@angular/core';

export interface Toast {
  id: number;
  text: string;
}

@Service()
export class ToastService {
  private nextId = 1;
  private readonly toasts = signal<Toast[]>([]);
  readonly items = this.toasts.asReadonly();

  show(text: string, durationMs = 3000): void {
    const toast: Toast = { id: this.nextId++, text };
    this.toasts.update((list) => [...list, toast]);
    setTimeout(() => this.dismiss(toast.id), durationMs);
  }

  dismiss(id: number): void {
    this.toasts.update((list) => list.filter((t) => t.id !== id));
  }
}
