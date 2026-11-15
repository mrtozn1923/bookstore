import { CurrencyPipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, HostListener, computed, inject, signal } from '@angular/core';
import { BookApi, type BookDto, type OrderDto } from '../api/book-api.service';
import { FilePicker } from './file-picker';

interface BulkLine {
  isbn: string;
  title: string;
  quantity: number;
}

const minimumCopies = 20;

@Component({
  selector: 'app-bulk-order-page',
  imports: [CurrencyPipe, FilePicker],
  template: `
    <h1>Bulk order</h1>
    <app-file-picker
      label="Order file"
      accept=".csv,text/csv"
      hint="Save the sheet from Excel as CSV with the columns isbn and quantity."
      (picked)="load($event)"
    />

    @if (lines().length > 0) {
      <table class="cart-lines" aria-label="Order lines">
        <thead>
          <tr><th scope="col">ISBN</th><th scope="col">Title</th><th scope="col">Quantity</th></tr>
        </thead>
        <tbody>
          @for (line of lines(); track line.isbn) {
            <tr><td>{{ line.isbn }}</td><td>{{ line.title }}</td><td>{{ line.quantity }}</td></tr>
          }
        </tbody>
      </table>
      <p class="summary">{{ lines().length }} lines, {{ copies() }} copies, 10% institutional discount</p>
    }

    <h2>Delivery</h2>
    <iframe class="slot-widget" title="Delivery slot" src="/widgets/delivery-slot.html"></iframe>
    @if (slot(); as reserved) {
      <p class="slot">Delivery slot: {{ reserved }}</p>
    }

    <p>
      <label><input type="checkbox" [checked]="accepted()" (change)="accepted.set($any($event.target).checked)" />
        I accept the terms of sale</label>
    </p>
    <button type="button" class="btn-primary" [disabled]="!canSubmit()" (click)="submit()">Submit order</button>

    @if (order(); as placed) {
      <p class="placed" role="status">Order {{ placed.reference }} submitted. Total: {{ placed.total | currency: 'TRY' : 'symbol-narrow' }}</p>
      <button type="button" (click)="downloadInvoice(placed)">Download invoice</button>
    }
    @if (problem(); as message) {
      <p class="error" role="alert">{{ message }}</p>
    }
  `,
})
export class BulkOrderPage {
  private readonly api = inject(BookApi);
  private readonly catalog = signal<BookDto[]>([]);
  protected readonly lines = signal<BulkLine[]>([]);
  protected readonly slot = signal<string | null>(null);
  protected readonly accepted = signal(false);
  protected readonly submitting = signal(false);
  protected readonly order = signal<OrderDto | null>(null);
  protected readonly problem = signal<string | null>(null);
  protected readonly copies = computed(() => this.lines().reduce((sum, line) => sum + line.quantity, 0));
  protected readonly canSubmit = computed(
    () => this.copies() >= minimumCopies && this.slot() !== null && this.accepted() && !this.submitting(),
  );

  constructor() {
    this.api.search('').subscribe((books) => this.catalog.set(books));
  }

  // Kargo firmasının iframe'deki widget'ı seçilen günü postMessage ile bildirir
  @HostListener('window:message', ['$event'])
  protected onMessage(event: MessageEvent): void {
    if (event.origin === location.origin && event.data?.type === 'delivery-slot') {
      this.slot.set(event.data.value);
    }
  }

  protected async load(file: File): Promise<void> {
    this.problem.set(null);
    this.order.set(null);
    const rows = (await file.text()).split(/\r?\n/).map((row) => row.trim()).filter((row) => row.length > 0);
    const parsed: BulkLine[] = [];
    for (const row of rows.slice(1)) {
      const [isbn, quantity] = row.split(/[;,]/).map((cell) => cell.trim());
      const book = this.catalog().find((b) => b.isbn === isbn);
      if (!book) {
        this.lines.set([]);
        this.problem.set(`Unknown ISBN ${isbn}.`);
        return;
      }
      parsed.push({ isbn, title: book.title, quantity: Number(quantity) });
    }
    this.lines.set(parsed);
    if (this.copies() < minimumCopies) {
      this.problem.set(`Institutional orders need at least ${minimumCopies} copies.`);
    }
  }

  protected submit(): void {
    if (!confirm(`Submit a bulk order for ${this.copies()} copies?`)) {
      return;
    }
    this.submitting.set(true);
    this.problem.set(null);
    const request = { lines: this.lines().map(({ isbn, quantity }) => ({ isbn, quantity })) };
    this.api.placeBulkOrder(request).subscribe({
      next: (order) => this.order.set(order),
      error: (response: HttpErrorResponse) => {
        this.submitting.set(false);
        this.problem.set(response.error?.title ?? 'The order could not be submitted.');
      },
    });
  }

  protected downloadInvoice(order: OrderDto): void {
    // Token header'la gittiği için link yerine blob indirilir ve geçici bir a[download] ile kaydedilir
    this.api.invoice(order.id).subscribe((blob) => {
      const url = URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = `invoice-${order.reference}.pdf`;
      link.click();
      setTimeout(() => URL.revokeObjectURL(url), 1000);
    });
  }
}
