import { CdkDrag, CdkDragDrop, CdkDragHandle, CdkDragPlaceholder, CdkDropList, moveItemInArray } from '@angular/cdk/drag-drop';
import { HttpClient } from '@angular/common/http';
import { afterNextRender, Component, ElementRef, inject, Injector, signal, viewChildren } from '@angular/core';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { BookSummary } from '../core/api-models';

// "My reading list": drag to sort, or use the move buttons; every new order is saved to the API
@Component({
  selector: 'app-reading-list-page',
  imports: [CdkDropList, CdkDrag, CdkDragHandle, CdkDragPlaceholder, RouterLink],
  templateUrl: './reading-list-page.html',
  styleUrl: './reading-list-page.css',
})
export class ReadingListPage {
  private readonly http = inject(HttpClient);
  private readonly injector = inject(Injector);
  private readonly moveButtons = viewChildren<ElementRef<HTMLButtonElement>>('move');

  protected readonly books = signal<BookSummary[]>([]);
  protected readonly loaded = signal(false);
  protected readonly status = signal('');

  constructor() {
    void firstValueFrom(this.http.get<BookSummary[]>('/api/reading-list')).then(books => {
      this.books.set(books);
      this.loaded.set(true);
    });
  }

  protected dropped(event: CdkDragDrop<BookSummary[]>): void {
    this.move(event.previousIndex, event.currentIndex);
  }

  // Keyboard and single-pointer alternative to dragging (WCAG 2.2, 2.5.7)
  protected moveBy(index: number, delta: -1 | 1, direction: 'up' | 'down'): void {
    const target = index + delta;
    if (target < 0 || target >= this.books().length) {
      return;
    }
    const isbn = this.books()[index].isbn;
    this.move(index, target);
    // If Angular moved the focused row itself in the DOM, focus fell to <body>: put it back on the same button
    afterNextRender(() => {
      const button = this.moveButtons().find(b => b.nativeElement.dataset['isbn'] === isbn && b.nativeElement.dataset['dir'] === direction);
      (button ?? this.moveButtons().find(b => b.nativeElement.dataset['isbn'] === isbn))?.nativeElement.focus();
    }, { injector: this.injector });
  }

  private move(from: number, to: number): void {
    if (from === to) {
      return;
    }
    const previous = this.books();
    // A new array with the same book objects: @for (track isbn) moves rows instead of re-creating them
    const next = [...previous];
    moveItemInArray(next, from, to);
    this.books.set(next);
    this.status.set(`${next[to].title} moved to position ${to + 1} of ${next.length}`);
    void this.save(next, previous);
  }

  private async save(next: BookSummary[], previous: BookSummary[]): Promise<void> {
    try {
      await firstValueFrom(this.http.put('/api/reading-list/order', { isbns: next.map(b => b.isbn) }));
    } catch {
      // Optimistic update: show the old order again if the API refused the new one
      this.books.set(previous);
      this.status.set('The new order could not be saved');
    }
  }
}
