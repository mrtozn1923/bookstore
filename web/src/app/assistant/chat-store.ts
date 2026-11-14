import { Injectable, inject, signal } from '@angular/core';
import { ChatApi } from './chat-api';

export type MessageStatus = 'thinking' | 'streaming' | 'done' | 'stopped' | 'interrupted';

export interface ChatMessage {
  id: number;
  role: 'user' | 'assistant';
  text: string;
  status: MessageStatus;
}

export interface Usage {
  inputTokens: number | null;
  outputTokens: number | null;
}

@Injectable({ providedIn: 'root' })
export class ChatStore {
  private readonly api = inject(ChatApi);
  private nextId = 1;
  private controller: AbortController | null = null;
  private pendingText = '';
  private frame = 0;

  readonly messages = signal<ChatMessage[]>([]);
  readonly busy = signal(false);
  readonly usage = signal<Usage | null>(null);

  async send(text: string): Promise<void> {
    if (this.busy()) {
      return;
    }
    const userId = this.nextId++;
    const id = this.nextId++;
    this.messages.update((list) => [
      ...list,
      { id: userId, role: 'user', text, status: 'done' },
      { id, role: 'assistant', text: '', status: 'thinking' },
    ]);
    this.busy.set(true);
    this.controller = new AbortController();
    let finished = false;
    try {
      for await (const event of this.api.stream(text, this.controller.signal)) {
        switch (event.type) {
          case 'delta':
            this.queueDelta(id, event.text);
            break;
          case 'usage':
            this.usage.set({ inputTokens: event.inputTokens, outputTokens: event.outputTokens });
            break;
          case 'done':
            finished = true;
            this.flush(id);
            this.patch(id, { status: 'done' });
            break;
          case 'error':
            finished = true;
            this.flush(id);
            this.patch(id, { status: 'interrupted' });
            break;
        }
      }
      // done gelmeden kapanan bağlantı "bitti" değil, "kesildi" demektir
      if (!finished) {
        this.flush(id);
        this.patch(id, { status: 'interrupted' });
      }
    } catch (err) {
      this.flush(id);
      const stopped = err instanceof DOMException && err.name === 'AbortError';
      this.patch(id, { status: stopped ? 'stopped' : 'interrupted' });
    } finally {
      this.busy.set(false);
      this.controller = null;
    }
  }

  stop(): void {
    this.controller?.abort();
  }

  // Delta'ları biriktirip ekrana kare başına bir kez yaz
  private queueDelta(id: number, text: string): void {
    this.pendingText += text;
    if (this.frame === 0) {
      this.frame = requestAnimationFrame(() => this.flush(id));
    }
  }

  private flush(id: number): void {
    cancelAnimationFrame(this.frame);
    this.frame = 0;
    if (this.pendingText === '') {
      return;
    }
    const text = this.pendingText;
    this.pendingText = '';
    this.messages.update((list) =>
      list.map((m) => (m.id === id ? { ...m, text: m.text + text, status: 'streaming' } : m)),
    );
  }

  private patch(id: number, change: Partial<ChatMessage>): void {
    this.messages.update((list) => list.map((m) => (m.id === id ? { ...m, ...change } : m)));
  }
}
