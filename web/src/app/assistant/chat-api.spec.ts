import { TestBed } from '@angular/core/testing';
import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';
import { DUNE_SHORT } from '../../testing/recorded-streams';
import { ChatApi, ChatHttpError } from './chat-api';
import { ChatEvent } from './chat-events';
import { ChatStore } from './chat-store';

// Gövdeyi verilen parçalara bölüp parçaların arasına bekleme koyan bir event-stream cevabı
function eventStream(chunks: string[], delayMs = 5, onCancel?: () => void): HttpResponse<ReadableStream> {
  const encoder = new TextEncoder();
  let timer: ReturnType<typeof setTimeout> | undefined;
  const body = new ReadableStream({
    async start(controller) {
      for (const [i, chunk] of chunks.entries()) {
        if (i > 0) {
          await new Promise((resolve) => (timer = setTimeout(resolve, delayMs)));
        }
        controller.enqueue(encoder.encode(chunk));
      }
      controller.close();
    },
    cancel() {
      clearTimeout(timer);
      onCancel?.();
    },
  });
  return new HttpResponse(body, { headers: { 'Content-Type': 'text/event-stream' } });
}

// Bir string'i n karakterlik parçalara böler: ağdaki rastgele chunk sınırlarını taklit eder
function split(text: string, size: number): string[] {
  return Array.from({ length: Math.ceil(text.length / size) }, (_, i) => text.slice(i * size, (i + 1) * size));
}

const server = setupServer();
beforeAll(() => server.listen({ onUnhandledFrame: 'error' }));
afterEach(() => server.resetHandlers());
afterAll(() => server.close());

async function collect(events: AsyncGenerator<ChatEvent>): Promise<ChatEvent[]> {
  const result: ChatEvent[] = [];
  for await (const event of events) {
    result.push(event);
  }
  return result;
}

describe('ChatApi over the network (MSW)', () => {
  const request = { message: 'Which is longer, Dune or The Hobbit?', conversationId: null, replaceLastTurn: false };

  it('replays a recorded stream split into 7-character chunks', async () => {
    server.use(http.post('/chat/sse', () => eventStream(split(DUNE_SHORT, 7), 1)));
    const events = await collect(new ChatApi().stream(request, new AbortController().signal));
    const text = events.map((e) => (e.type === 'delta' ? e.text : '')).join('');
    expect(events.map((e) => e.type).filter((t) => t !== 'delta')).toEqual(['conversation', 'sources', 'thinking', 'usage', 'done']);
    expect(text).toMatch(/^Dune is longer than The Hobbit/);
  });

  it('turns 429 into ChatHttpError with Retry-After', async () => {
    server.use(
      http.post('/chat/sse', () =>
        HttpResponse.json({ title: 'Too many requests', status: 429, detail: 'Please wait.' }, { status: 429, headers: { 'Retry-After': '60' } }),
      ),
    );
    const error = await collect(new ChatApi().stream(request, new AbortController().signal)).catch((e: unknown) => e);
    expect(error).toBeInstanceOf(ChatHttpError);
    expect(error).toMatchObject({ status: 429, detail: 'Please wait.', retryAfterSeconds: 60 });
  });

  it('cancels the response body when the caller aborts', async () => {
    let cancelled = false;
    server.use(http.post('/chat/sse', () => eventStream(['event: thinking\ndata: {}\n\n', 'event: delta\ndata: {"text":"never"}\n\n'], 10_000, () => (cancelled = true))));
    const controller = new AbortController();
    const events = new ChatApi().stream(request, controller.signal);
    const first = await events.next();
    controller.abort();
    const rest = await collect(events).catch((e: unknown) => e);
    expect(first.value).toEqual({ type: 'thinking' });
    expect(rest).toMatchObject({ name: 'AbortError' });
    expect(cancelled).toBe(true);
  });

  it('drives the store end to end without a model', async () => {
    server.use(http.post('/chat/sse', () => eventStream(split(DUNE_SHORT, 50), 1)));
    sessionStorage.clear();
    const store = TestBed.inject(ChatStore);
    await store.send('Which is longer, Dune or The Hobbit?');
    expect(store.status()).toBe('done');
    expect(store.conversationId()).toBe('f38c7df6-ac75-45a7-ac34-d3ec5be2e852');
    expect(store.messages()[1].sources.map((s) => s.title)).toContain('Dune');
  });
});
