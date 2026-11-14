import { CurrencyPipe } from '@angular/common';
import { httpResource } from '@angular/common/http';
import { Component, inject, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Book } from '../catalog/book';
import { DeviceCapabilities } from '../core/device-capabilities';
import { ReadingListStore } from '../reading-list/reading-list-store';
import { ReviewOutbox } from '../reviews/review-outbox';

@Component({
  selector: 'app-book-page',
  imports: [CurrencyPipe, RouterLink],
  template: `
    @if (book.hasValue()) {
      <h1>{{ book.value().title }}</h1>
      <p>{{ book.value().author }}, {{ book.value().price | currency: 'TRY' }}</p>
      <button (click)="readingList.add(book.value())">Add to reading list</button>
      <a [routerLink]="['/books', isbn(), 'preview']">Read a sample</a>
      <!-- no share sheet and no clipboard: no button at all -->
      @if (device.supports.share || device.supports.clipboard) {
        <button class="share" (click)="share(book.value())">
          {{ device.supports.share ? 'Share' : 'Copy link' }}
        </button>
      }
      @if (device.supports.clipboardImage) {
        <button class="copy-card" (click)="copyCard(book.value())">Copy as image</button>
      }
      @if (shareMessage()) {
        <p class="share-message">{{ shareMessage() }}</p>
      }

      <form
        (submit)="$event.preventDefault(); review(rating.value, comment.value); comment.value = ''"
      >
        <select #rating>
          @for (n of [5, 4, 3, 2, 1]; track n) {
            <option [value]="n">{{ n }} stars</option>
          }
        </select>
        <textarea #comment placeholder="Your review"></textarea>
        <button type="submit">Send review</button>
      </form>
      @if (outbox.pending()) {
        <p class="pending">{{ outbox.pending() }} review(s) waiting for a connection</p>
      }
    } @else if (book.error()) {
      <p>This book could not be loaded.</p>
    }

    <h2>Reading list</h2>
    <ul class="reading-list">
      @for (item of readingList.items.value(); track item.isbn) {
        <li>{{ item.title }}</li>
      }
    </ul>
  `,
})
export class BookPage {
  readonly isbn = input.required<string>();
  protected readonly book = httpResource<Book>(() => `/api/books/${this.isbn()}`);
  protected readonly readingList = inject(ReadingListStore);
  protected readonly outbox = inject(ReviewOutbox);
  protected readonly device = inject(DeviceCapabilities);
  protected readonly shareMessage = signal('');

  async share(book: Book) {
    const url = `${location.origin}/books/${book.isbn}`;
    const result = await this.device.share({
      title: book.title,
      text: `${book.title} by ${book.author}`,
      url,
    });
    console.log(`share: ${result}`);
    const messages = {
      shared: '',
      cancelled: '',
      copied: 'Link copied.',
      failed: 'Copy this link: ' + url,
    };
    this.shareMessage.set(messages[result]);
  }

  async copyCard(book: Book) {
    const copied = await this.device.copyImage(bookCard(book));
    console.log(`copy card: ${copied}`);
    this.shareMessage.set(copied ? 'Image copied.' : 'The image could not be copied.');
  }

  review(rating: string, comment: string) {
    void this.outbox.enqueue(this.isbn(), Number(rating), comment);
  }
}

// A small PNG "card" for chat apps: title, author and price drawn on a canvas.
function bookCard(book: Book): Promise<Blob> {
  const canvas = document.createElement('canvas');
  canvas.width = 600;
  canvas.height = 300;
  const g = canvas.getContext('2d')!;
  g.fillStyle = '#1e3a5f';
  g.fillRect(0, 0, 600, 300);
  g.fillStyle = '#ffffff';
  g.font = 'bold 44px sans-serif';
  g.fillText(book.title, 32, 110);
  g.font = '28px sans-serif';
  g.fillText(book.author, 32, 160);
  g.fillText(`${book.price} TRY at Bookstore`, 32, 250);
  return new Promise((resolve, reject) =>
    canvas.toBlob(
      (blob) => (blob ? resolve(blob) : reject(new Error('toBlob failed'))),
      'image/png',
    ),
  );
}
