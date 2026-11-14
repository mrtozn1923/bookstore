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
  async *stream(request: ChatRequest, signal: AbortSignal): AsyncGenerator<ChatEvent> {
    const response = await fetch('/chat/sse', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', Accept: 'text/event-stream' },
      body: JSON.stringify(request),
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

  async history(conversationId: string): Promise<HistoryItem[] | null> {
    const response = await fetch(`/chat/conversations/${conversationId}/messages`);
    return response.ok ? response.json() : null;
  }
}
