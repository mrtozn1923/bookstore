import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { HttpAgent, Message } from '@ag-ui/client';
import { GenerativeBlock } from '../assistant/generative-block';
import { MessageBody } from '../assistant/message-body';
import { UiBlock, parseUiBlock } from '../assistant/ui-blocks';

type RunStatus = 'idle' | 'thinking' | 'streaming' | 'error';

// AG-UI 1.0'da tool sonucu string ya da content part listesi olabilir; burada yalnız JSON metni bekliyoruz
function parseJson(content: unknown): unknown {
  if (typeof content !== 'string') {
    return null;
  }
  try {
    return JSON.parse(content);
  } catch {
    return null;
  }
}

// 2. yazıdaki chat, AG-UI'ın HttpAgent'ıyla: mesaj listesini ve event sırasını library tutar
@Component({
  selector: 'app-agui-chat',
  imports: [MessageBody, GenerativeBlock],
  template: `
    <section class="log">
      @for (message of messages(); track message.id) {
        @if (message.role === 'tool') {
          @if (blocks().get(message.toolCallId); as block) {
            <app-generative-block [block]="block" />
          }
        } @else if ((message.role === 'user' || message.role === 'assistant') && textOf(message) !== '') {
          <article class="bubble" [class.user]="message.role === 'user'">
            @if (message.role === 'assistant') {
              <app-message-body [text]="textOf(message)" [final]="!running()" />
            } @else {
              <p class="text">{{ textOf(message) }}</p>
            }
          </article>
        }
      }
      @if (status() === 'thinking') {
        <p class="status">Thinking…</p>
      }
    </section>
    @if (error(); as error) {
      <p class="banner">{{ error }}</p>
    }
    <form (submit)="submit($event, input)">
      <input #input placeholder="Ask the Bookstore assistant (AG-UI)" />
      @if (running()) {
        <button type="button" (click)="stop()">Stop</button>
      } @else {
        <button type="submit">Send</button>
      }
    </form>
  `,
  styleUrl: '../assistant/assistant.css',
})
export class AgUiChat {
  private readonly agent = new HttpAgent({ url: '/agui' });
  protected readonly messages = signal<readonly Message[]>([]);
  protected readonly status = signal<RunStatus>('idle');
  protected readonly running = signal(false);
  protected readonly error = signal<string | null>(null);

  // AG-UI'da ayrı bir "ui" event'i yok: show_books'un TOOL_CALL_RESULT'ı aynı registry'den geçip component olur
  protected readonly blocks = computed(() => {
    const names = new Map(
      this.messages().flatMap((m) => (m.role === 'assistant' ? (m.toolCalls ?? []) : [])).map((c) => [c.id, c.function.name]),
    );
    const blocks = new Map<string, UiBlock>();
    for (const m of this.messages()) {
      if (m.role === 'tool' && names.get(m.toolCallId) === 'show_books') {
        const block = parseUiBlock('book-list', parseJson(m.content));
        if (block) {
          blocks.set(m.toolCallId, block);
        }
      }
    }
    return blocks;
  });

  constructor() {
    // AG-UI event'leri subscriber callback'leriyle gelir; signal'lara yazmak yeterli
    const subscription = this.agent.subscribe({
      onMessagesChanged: ({ messages }) => this.messages.set(messages),
      onReasoningStartEvent: () => this.status.set('thinking'),
      onTextMessageStartEvent: () => this.status.set('streaming'),
      onRunErrorEvent: ({ event }) => {
        // abortRun'ı istemci kendisi code: 'abort' olan bir RUN_ERROR'a çeviriyor; durdurmak bir hata değil (3. yazı)
        if (event.code === 'abort') {
          this.status.set('idle');
          return;
        }
        this.status.set('error');
        this.error.set(event.message);
      },
    });
    inject(DestroyRef).onDestroy(() => subscription.unsubscribe());
  }

  protected textOf(message: Message): string {
    return typeof message.content === 'string' ? message.content : '';
  }

  protected async submit(event: Event, input: HTMLInputElement): Promise<void> {
    event.preventDefault();
    const text = input.value.trim();
    if (text === '' || this.running()) {
      return;
    }
    input.value = '';
    this.error.set(null);
    this.agent.addMessage({ id: crypto.randomUUID(), role: 'user', content: text });
    this.running.set(true);
    try {
      await this.agent.runAgent();
      this.status.set('idle');
    } catch (err) {
      this.status.set('error');
      this.error.set(err instanceof Error ? err.message : String(err));
    } finally {
      this.running.set(false);
    }
  }

  protected stop(): void {
    this.agent.abortRun();
  }
}
