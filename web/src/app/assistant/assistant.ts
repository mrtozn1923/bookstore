import { LiveAnnouncer } from '@angular/cdk/a11y';
import { Component, DestroyRef, ElementRef, afterRenderEffect, effect, inject, signal, viewChild } from '@angular/core';
import { ApprovalCard, describe } from './approval-card';
import { ChatStatus, ChatStore } from './chat-store';
import { GenerativeBlock } from './generative-block';
import { MessageBody } from './message-body';

// Tool çalışırken kullanıcıya gösterilen durum satırları
const TOOL_LABELS: Record<string, string> = {
  show_books: 'Looking through the catalog…',
  cancel_order: 'Cancelling the order…',
};

// Ekran okuyucuya markdown işaretleri ve [n] atıfları okunmasın
function plainText(markdown: string): string {
  return markdown
    .replace(/\[(\d+)\]/g, '')
    .replace(/[*_`#>]/g, '')
    .replace(/\s+/g, ' ')
    .trim();
}

@Component({
  selector: 'app-assistant',
  imports: [MessageBody, GenerativeBlock, ApprovalCard],
  templateUrl: './assistant.html',
  styleUrl: './assistant.css',
  host: { '(keydown.escape)': 'store.stop()' },
})
export class Assistant {
  protected readonly store = inject(ChatStore);
  private readonly announcer = inject(LiveAnnouncer);
  protected readonly draft = signal('');
  protected readonly editing = signal(false);
  protected readonly elapsed = signal(0);
  private readonly log = viewChild.required<ElementRef<HTMLElement>>('log');
  private readonly input = viewChild.required<ElementRef<HTMLInputElement>>('input');
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

    // Akan metin canlı bölgede değil; ekran okuyucuya yalnız tur bitince tek bir duyuru gider
    let previous: ChatStatus = this.store.status();
    effect(() => {
      const status = this.store.status();
      if (status === previous) {
        return;
      }
      previous = status;
      const last = this.store.messages().at(-1);
      if (status === 'done' && last?.role === 'assistant') {
        const approval = last.approval?.state === 'pending' ? last.approval : null;
        const text = approval ? `Approval needed. ${describe(approval.tool, approval.arguments)}` : plainText(last.text);
        void this.announcer.announce(`Assistant: ${text}`, 'polite');
      } else if (status === 'aborted') {
        void this.announcer.announce('Stopped.', 'polite');
      }
    });

    // Bekleme süresini saniye saniye göster; ilk saniyelerde gösterme
    let timer = 0;
    effect(() => {
      clearInterval(timer);
      this.elapsed.set(0);
      if (this.store.busy()) {
        timer = setInterval(() => this.elapsed.update((s) => s + 1), 1000);
      }
    });
    inject(DestroyRef).onDestroy(() => clearInterval(timer));

    // Send/Stop butonu yer değiştirince odak body'ye düşmesin
    afterRenderEffect(() => {
      this.store.busy();
      if (document.activeElement === document.body) {
        this.input().nativeElement.focus();
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
    this.input().nativeElement.focus();
  }

  protected submit(event: Event): void {
    event.preventDefault();
    const text = this.draft().trim();
    if (text === '' || !this.store.canSend()) {
      return;
    }
    this.draft.set('');
    this.stickToBottom = true;
    // Gönderdikten sonra odak input'ta kalır; kullanıcı bir sonraki soruyu hemen yazabilir
    this.input().nativeElement.focus();
    if (this.editing()) {
      this.editing.set(false);
      void this.store.editLast(text);
    } else {
      void this.store.send(text);
    }
  }
}
