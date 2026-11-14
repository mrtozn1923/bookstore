import { Component, ElementRef, afterRenderEffect, inject, signal, viewChild } from '@angular/core';
import { ApprovalCard } from './approval-card';
import { ChatStore } from './chat-store';
import { GenerativeBlock } from './generative-block';
import { MessageBody } from './message-body';

// Tool çalışırken kullanıcıya gösterilen durum satırları
const TOOL_LABELS: Record<string, string> = {
  show_books: 'Looking through the catalog…',
  cancel_order: 'Cancelling the order…',
};

@Component({
  selector: 'app-assistant',
  imports: [MessageBody, GenerativeBlock, ApprovalCard],
  templateUrl: './assistant.html',
  styleUrl: './assistant.css',
})
export class Assistant {
  protected readonly store = inject(ChatStore);
  protected readonly draft = signal('');
  protected readonly editing = signal(false);
  private readonly log = viewChild.required<ElementRef<HTMLElement>>('log');
  private stickToBottom = true;

  constructor() {
    void this.store.restore();
    // DOM güncellendikten sonra çalışır: kullanıcı yukarı kaydırmadıysa en alta in
    afterRenderEffect(() => {
      this.store.messages();
      const el = this.log().nativeElement;
      if (this.stickToBottom) {
        el.scrollTop = el.scrollHeight;
      }
    });
  }

  protected toolLabel(name: string): string {
    return TOOL_LABELS[name] ?? 'Working on it…';
  }

  protected onScroll(): void {
    const el = this.log().nativeElement;
    this.stickToBottom = el.scrollHeight - el.scrollTop - el.clientHeight < 40;
  }

  protected startEdit(text: string): void {
    this.draft.set(text);
    this.editing.set(true);
  }

  protected submit(event: Event): void {
    event.preventDefault();
    const text = this.draft().trim();
    if (text === '' || !this.store.canSend()) {
      return;
    }
    this.draft.set('');
    this.stickToBottom = true;
    if (this.editing()) {
      this.editing.set(false);
      void this.store.editLast(text);
    } else {
      void this.store.send(text);
    }
  }
}
