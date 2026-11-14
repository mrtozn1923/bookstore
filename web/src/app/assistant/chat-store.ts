import { Injectable, computed, inject, signal } from '@angular/core';
import { ChatApi, ChatHttpError } from './chat-api';
import { ApprovalRequest } from './approval-card';
import { ChatEvent, Source } from './chat-events';
import { UiBlock, parseUiBlock } from './ui-blocks';

export type ChatStatus = 'idle' | 'submitted' | 'streaming' | 'done' | 'error' | 'aborted';

// İzin verilen geçişler; listede olmayan geçiş bir hatadır
const TRANSITIONS: Record<ChatStatus, readonly ChatStatus[]> = {
  idle: ['submitted'],
  submitted: ['streaming', 'error', 'aborted'],
  streaming: ['done', 'error', 'aborted'],
  done: ['submitted'],
  error: ['submitted'],
  aborted: ['submitted'],
};

export type ErrorKind = 'network' | 'timeout' | 'rate-limit' | 'bad-request' | 'server' | 'stream';

export interface ChatError {
  kind: ErrorKind;
  message: string;
}

export interface ChatMessage {
  id: number;
  role: 'user' | 'assistant';
  text: string;
  // Asistan mesajının nasıl bittiği; kesilen cevap silinmez, işaretlenir
  ending: 'complete' | 'interrupted' | 'stopped' | null;
  sources: Source[];
  tools: ToolActivity[];
  blocks: UiBlock[];
  approval: ApprovalRequest | null;
}

export interface ToolActivity {
  callId: string;
  name: string;
  phase: 'started' | 'completed';
}

export interface Usage {
  inputTokens: number | null;
  outputTokens: number | null;
}

const FIRST_TOKEN_TIMEOUT_MS = 30_000;
// Sunucu 5 saniyede bir ping gönderiyor; 15 saniye hiçbir event gelmezse bağlantı ölü sayılır
const SILENCE_TIMEOUT_MS = 15_000;
const CONVERSATION_KEY = 'bookstore.conversationId';

@Injectable({ providedIn: 'root' })
export class ChatStore {
  private readonly api = inject(ChatApi);
  private nextId = 1;
  private controller: AbortController | null = null;
  private pendingText = '';
  private frame = 0;
  private cooldownTimer = 0;

  readonly status = signal<ChatStatus>('idle');
  readonly messages = signal<ChatMessage[]>([]);
  readonly error = signal<ChatError | null>(null);
  readonly usage = signal<Usage | null>(null);
  readonly cooldown = signal(0);
  readonly conversationId = signal<string | null>(sessionStorage.getItem(CONVERSATION_KEY));

  readonly busy = computed(() => this.status() === 'submitted' || this.status() === 'streaming');
  readonly canSend = computed(() => !this.busy() && this.cooldown() === 0);
  readonly canRegenerate = computed(
    () => this.canSend() && this.messages().at(-1)?.role === 'assistant',
  );

  // Sayfa yenilenince sunucudaki geçmişi yükle
  async restore(): Promise<void> {
    const id = this.conversationId();
    if (id === null || this.messages().length > 0) {
      return;
    }
    const history = await this.api.history(id);
    if (history === null) {
      this.newConversation();
      return;
    }
    this.messages.set(
      history.map((h) => ({
        id: this.nextId++,
        role: h.role,
        text: h.text,
        ending: h.role === 'assistant' ? 'complete' : null,
        sources: [],
        tools: [],
        blocks: [],
        approval: null,
      })),
    );
  }

  send(text: string): Promise<void> {
    return this.run(text, false);
  }

  // Son cevabı yeniden üret: tur sunucuda saklandıysa onu değiştir
  regenerate(): Promise<void> {
    const list = this.messages();
    const answer = list.at(-1);
    const question = list.at(-2);
    if (!this.canRegenerate() || answer?.role !== 'assistant' || question?.role !== 'user') {
      return Promise.resolve();
    }
    this.messages.set(list.slice(0, -2));
    return this.run(question.text, answer.ending === 'complete');
  }

  // Son soruyu düzenleyip yeniden gönder
  editLast(text: string): Promise<void> {
    const list = this.messages();
    const answer = list.at(-1);
    if (!this.canRegenerate() || answer?.role !== 'assistant') {
      return Promise.resolve();
    }
    this.messages.set(list.slice(0, -2));
    return this.run(text, answer.ending === 'complete');
  }

  stop(): void {
    this.controller?.abort();
  }

  newConversation(): void {
    if (this.busy()) {
      return;
    }
    sessionStorage.removeItem(CONVERSATION_KEY);
    this.conversationId.set(null);
    this.messages.set([]);
    this.error.set(null);
    this.usage.set(null);
  }

  private async run(text: string, replaceLastTurn: boolean): Promise<void> {
    if (!this.canSend() || !this.to('submitted')) {
      return;
    }
    const id = this.addTurn(text);
    const request = { message: text, conversationId: this.conversationId(), replaceLastTurn };
    await this.consume(id, (signal) => this.api.stream(request, signal));
  }

  // Onay kartındaki karar: sunucu aynı turu sürdürür, cevap aynı asistan mesajına eklenir
  async respond(messageId: number, approved: boolean): Promise<void> {
    const approval = this.messages().find((m) => m.id === messageId)?.approval;
    if (!approval || approval.state !== 'pending' || !this.canSend() || !this.to('submitted')) {
      return;
    }
    this.patch(messageId, { approval: { ...approval, state: approved ? 'approved' : 'rejected' }, ending: null });
    await this.consume(messageId, (signal) => this.api.approve(approval.approvalId, approved, signal));
  }

  private async consume(id: number, open: (signal: AbortSignal) => AsyncGenerator<ChatEvent>): Promise<void> {
    this.error.set(null);
    const controller = new AbortController();
    this.controller = controller;
    // İlk token gelmezse isteği "timeout" nedeniyle iptal et
    const timer = setTimeout(
      () => controller.abort(new DOMException('No answer within 30 seconds.', 'TimeoutError')),
      FIRST_TOKEN_TIMEOUT_MS,
    );
    let silence = 0;
    const resetSilence = () => {
      clearTimeout(silence);
      silence = setTimeout(
        () => controller.abort(new DOMException('No event within 15 seconds.', 'NetworkError')),
        SILENCE_TIMEOUT_MS,
      );
    };
    resetSilence();
    let ended = false;
    try {
      for await (const event of open(controller.signal)) {
        resetSilence();
        // Modelden gelen ilk hayat belirtisi (düşünme, metin, tool, onay, component) akışı başlatır
        if (!['ping', 'conversation', 'sources', 'usage'].includes(event.type)) {
          clearTimeout(timer);
          if (this.status() === 'submitted') {
            this.to('streaming');
          }
        }
        switch (event.type) {
          case 'conversation':
            this.conversationId.set(event.id);
            sessionStorage.setItem(CONVERSATION_KEY, event.id);
            break;
          case 'sources':
            this.patch(id, { sources: event.items });
            break;
          case 'delta':
            this.queueDelta(id, event.text);
            break;
          case 'tool':
            this.update(id, (m) => ({
              tools: [...m.tools.filter((t) => t.callId !== event.callId), { callId: event.callId, name: event.name, phase: event.phase }],
            }));
            break;
          case 'ui': {
            // Sunucudan gelse bile props doğrulanmadan component'e verilmez
            const block = parseUiBlock(event.component, event.props);
            if (block) {
              this.update(id, (m) => ({ blocks: [...m.blocks, block] }));
            } else {
              console.warn(`Rejected UI block "${event.component}"`, event.props);
            }
            break;
          }
          case 'approval':
            this.patch(id, {
              approval: {
                approvalId: event.approvalId,
                tool: event.tool,
                arguments: event.arguments,
                expiresAt: Date.now() + event.expiresInSeconds * 1000,
                state: 'pending',
              },
            });
            break;
          case 'usage':
            this.usage.set({ inputTokens: event.inputTokens, outputTokens: event.outputTokens });
            break;
          case 'done':
            ended = true;
            this.finish(id, 'complete', 'done');
            break;
          case 'error':
            ended = true;
            this.fail(id, { kind: 'stream', message: event.message });
            break;
        }
      }
      if (!ended) {
        this.fail(id, { kind: 'network', message: 'The connection closed before the answer was complete.' });
      }
    } catch (err) {
      this.handleError(id, err);
    } finally {
      clearTimeout(timer);
      clearTimeout(silence);
      this.controller = null;
    }
  }

  private handleError(id: number, err: unknown): void {
    if (err instanceof DOMException && err.name === 'AbortError') {
      // Kullanıcı durdurdu: bu bir hata değil
      this.finish(id, 'stopped', 'aborted');
    } else if (err instanceof DOMException && err.name === 'TimeoutError') {
      this.fail(id, { kind: 'timeout', message: 'The assistant did not start answering in time.' });
    } else if (err instanceof ChatHttpError && err.status === 429) {
      this.fail(id, { kind: 'rate-limit', message: err.detail });
      this.startCooldown(err.retryAfterSeconds ?? 30);
    } else if (err instanceof ChatHttpError && err.status < 500) {
      this.fail(id, { kind: 'bad-request', message: err.detail });
    } else if (err instanceof ChatHttpError) {
      this.fail(id, { kind: 'server', message: 'The assistant is unavailable right now.' });
    } else {
      // fetch TypeError ya da sessizlik (NetworkError): bağlantı kurulamadı veya akış ortasında koptu
      this.fail(id, { kind: 'network', message: 'Connection lost. Check your network and try again.' });
    }
  }

  private to(next: ChatStatus): boolean {
    const current = this.status();
    if (!TRANSITIONS[current].includes(next)) {
      console.warn(`Ignored chat transition ${current} -> ${next}`);
      return false;
    }
    this.status.set(next);
    return true;
  }

  private addTurn(text: string): number {
    const userId = this.nextId++;
    const id = this.nextId++;
    this.messages.update((list) => [
      ...list,
      { id: userId, role: 'user', text, ending: null, sources: [], tools: [], blocks: [], approval: null },
      { id, role: 'assistant', text: '', ending: null, sources: [], tools: [], blocks: [], approval: null },
    ]);
    return id;
  }

  private finish(id: number, ending: 'complete' | 'stopped', status: ChatStatus): void {
    this.flush(id);
    this.patch(id, { ending });
    this.to(status);
  }

  private fail(id: number, error: ChatError): void {
    this.flush(id);
    this.patch(id, { ending: 'interrupted' });
    this.error.set(error);
    this.to('error');
  }

  private startCooldown(seconds: number): void {
    clearInterval(this.cooldownTimer);
    this.cooldown.set(seconds);
    this.cooldownTimer = setInterval(() => {
      this.cooldown.update((s) => Math.max(0, s - 1));
      if (this.cooldown() === 0) {
        clearInterval(this.cooldownTimer);
      }
    }, 1000);
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
    this.messages.update((list) => list.map((m) => (m.id === id ? { ...m, text: m.text + text } : m)));
  }

  private patch(id: number, change: Partial<ChatMessage>): void {
    this.messages.update((list) => list.map((m) => (m.id === id ? { ...m, ...change } : m)));
  }

  private update(id: number, change: (m: ChatMessage) => Partial<ChatMessage>): void {
    this.messages.update((list) => list.map((m) => (m.id === id ? { ...m, ...change(m) } : m)));
  }
}
