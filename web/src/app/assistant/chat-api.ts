import { Injectable } from '@angular/core';
import { ChatEvent, toChatEvent } from './chat-events';
import { readSse } from './sse';

@Injectable({ providedIn: 'root' })
export class ChatApi {
  // EventSource yalnız GET atar ve gövde gönderemez; mesajı POST ile göndermek için fetch kullanıyoruz
  async *stream(message: string, signal: AbortSignal): AsyncGenerator<ChatEvent> {
    const response = await fetch('/chat/sse', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', Accept: 'text/event-stream' },
      body: JSON.stringify({ message }),
      signal,
    });
    if (!response.ok || !response.body) {
      throw new Error(`Chat request failed with HTTP ${response.status}`);
    }
    for await (const { event, data } of readSse(response.body)) {
      const chatEvent = toChatEvent(event, data);
      if (chatEvent) {
        yield chatEvent;
      }
    }
  }
}
