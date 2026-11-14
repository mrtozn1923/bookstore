import { Component } from '@angular/core';
import { Chat } from '@ai-sdk/angular';
import { DefaultChatTransport, UIMessage } from 'ai';
import { MessageBody } from '../assistant/message-body';

// Aynı chat, AI SDK UI'ın Chat class'ıyla: messages, status ve error içeride signal
@Component({
  selector: 'app-ai-sdk-chat',
  imports: [MessageBody],
  template: `
    <section class="log">
      @for (message of chat.messages; track message.id) {
        <article class="bubble" [class.user]="message.role === 'user'">
          @if (message.role === 'assistant') {
            <app-message-body [text]="textOf(message)" [final]="chat.status === 'ready'" />
          } @else {
            <p class="text">{{ textOf(message) }}</p>
          }
        </article>
      }
      @if (chat.status === 'submitted') {
        <p class="status">Thinking…</p>
      }
    </section>
    @if (chat.error; as error) {
      <p class="banner">{{ error.message }}</p>
    }
    <form (submit)="submit($event, input)">
      <input #input placeholder="Ask the Bookstore assistant (AI SDK UI)" />
      @if (chat.status === 'submitted' || chat.status === 'streaming') {
        <button type="button" (click)="chat.stop()">Stop</button>
      } @else {
        <button type="submit">Send</button>
      }
    </form>
  `,
  styleUrl: '../assistant/assistant.css',
})
export class AiSdkChat {
  protected readonly chat = new Chat({
    transport: new DefaultChatTransport({ api: '/chat/ui-stream' }),
  });

  // Bir mesaj "parts" listesidir; burada yalnız metin parçalarını birleştiriyoruz
  protected textOf(message: UIMessage): string {
    return message.parts.map((p) => (p.type === 'text' ? p.text : '')).join('');
  }

  protected submit(event: Event, input: HTMLInputElement): void {
    event.preventDefault();
    const text = input.value.trim();
    if (text === '' || this.chat.status === 'submitted' || this.chat.status === 'streaming') {
      return;
    }
    input.value = '';
    void this.chat.sendMessage({ text });
  }
}
