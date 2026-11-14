import { Component, ElementRef, afterRenderEffect, inject, signal, viewChild } from '@angular/core';
import { ChatStore } from './chat-store';

@Component({
  selector: 'app-assistant',
  templateUrl: './assistant.html',
  styleUrl: './assistant.css',
})
export class Assistant {
  protected readonly store = inject(ChatStore);
  protected readonly draft = signal('');
  private readonly log = viewChild.required<ElementRef<HTMLElement>>('log');
  private stickToBottom = true;

  constructor() {
    // DOM güncellendikten sonra çalışır: kullanıcı yukarı kaydırmadıysa en alta in
    afterRenderEffect(() => {
      this.store.messages();
      const el = this.log().nativeElement;
      if (this.stickToBottom) {
        el.scrollTop = el.scrollHeight;
      }
    });
  }

  protected onScroll(): void {
    const el = this.log().nativeElement;
    this.stickToBottom = el.scrollHeight - el.scrollTop - el.clientHeight < 40;
  }

  protected submit(event: Event): void {
    event.preventDefault();
    const text = this.draft().trim();
    if (text === '') {
      return;
    }
    this.draft.set('');
    this.stickToBottom = true;
    void this.store.send(text);
  }
}
