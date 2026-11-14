import { Injectable } from '@angular/core';
import { ChatEvent, toChatEvent } from './chat-events';
import { readSse } from './sse';

export interface ChatRequest {
  message: string;
  conversationId: string | null;
  replaceLastTurn: boolean;
}

export interface HistoryItem {
  role: 'user' | 'assistant';
  text: string;
}

// Akış hiç başlamadan dönen HTTP hatası; gövde ProblemDetails (RFC 9457)
export class ChatHttpError extends Error {
  constructor(
    readonly status: number,
    readonly detail: string,
    readonly retryAfterSeconds: number | null,
  ) {
    super(`HTTP ${status}: ${detail}`);
  }
}

@Injectable({ providedIn: 'root' })
export class ChatApi {
  // EventSource yalnız GET atar ve gövde gönderemez; mesajı POST ile göndermek için fetch kullanıyoruz
  stream(request: ChatRequest, signal: AbortSignal): AsyncGenerator<ChatEvent> {
    return this.post('/chat/sse', request, signal);
  }

  // Onay kartındaki karar; sunucu bekleyen turu sürdürür ve aynı event akışını döner
  approve(approvalId: string, approved: boolean, signal: AbortSignal): AsyncGenerator<ChatEvent> {
    return this.post(`/chat/approvals/${encodeURIComponent(approvalId)}`, { approved }, signal);
  }

  private async *post(url: string, body: unknown, signal: AbortSignal): AsyncGenerator<ChatEvent> {
    const response = await fetch(url, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', Accept: 'text/event-stream' },
      body: JSON.stringify(body),
      signal,
    });
    if (!response.ok || !response.body) {
      const problem = await response.json().catch(() => null);
      const retryAfter = Number(response.headers.get('Retry-After'));
      throw new ChatHttpError(
        response.status,
        problem?.detail ?? response.statusText,
        Number.isFinite(retryAfter) && retryAfter > 0 ? retryAfter : null,
      );
    }
    for await (const { event, data } of readSse(response.body)) {
      const chatEvent = toChatEvent(event, data);
      if (chatEvent) {
        yield chatEvent;
      }
    }
  }

  // Geri bildirim akış değil; sonucu yalnız başarılı/başarısız
  async feedback(body: { conversationId: string; turn: number; rating: 'up' | 'down' }): Promise<boolean> {
    const response = await fetch('/chat/feedback', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body),
    });
    return response.ok;
  }

  async history(conversationId: string): Promise<HistoryItem[] | null> {
    const response = await fetch(`/chat/conversations/${conversationId}/messages`);
    return response.ok ? response.json() : null;
  }
}
