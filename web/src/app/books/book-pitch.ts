import { Component, ResourceStreamItem, inject, input, resource, signal } from '@angular/core';
import { ChatApi } from '../assistant/chat-api';

export interface Pitch {
  text: string;
  done: boolean;
}

@Component({
  selector: 'app-book-pitch',
  template: `
    @if (pitch.error(); as error) {
      <p class="error">No summary right now: {{ error.message }}</p>
    } @else if (pitch.hasValue()) {
      <p>{{ pitch.value().text }}@if (!pitch.value().done) {<span class="cursor">▍</span>}</p>
    } @else {
      <p class="muted">Generating a summary…</p>
    }
  `,
})
export class BookPitch {
  readonly title = input.required<string>();
  private readonly api = inject(ChatApi);

  // Tek istek, tek değer: kitap değişince önceki akış abortSignal ile iptal edilir
  protected readonly pitch = resource({
    params: () => ({ title: this.title() }),
    stream: async ({ params, abortSignal }) => {
      const result = signal<ResourceStreamItem<Pitch>>({ value: { text: '', done: false } });
      const prompt = `In one sentence, why should someone read "${params.title}"?`;
      void (async () => {
        let text = '';
        try {
          for await (const event of this.api.stream(prompt, abortSignal)) {
            if (event.type === 'delta') {
              text += event.text;
              result.set({ value: { text, done: false } });
            } else if (event.type === 'done') {
              result.set({ value: { text, done: true } });
            } else if (event.type === 'error') {
              result.set({ error: new Error(event.message) });
            }
          }
        } catch (err) {
          // Kitap değiştiği için iptal edildiyse hata gösterme
          if (!abortSignal.aborted) {
            result.set({ error: err instanceof Error ? err : new Error(String(err)) });
          }
        }
      })();
      return result;
    },
  });
}
