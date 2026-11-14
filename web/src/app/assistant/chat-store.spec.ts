import { TestBed } from '@angular/core/testing';
import { Step, pings, scripted } from '../../testing/fake-chat-stream';
import { ChatApi, ChatHttpError } from './chat-api';
import { ChatEvent } from './chat-events';
import { ChatStore } from './chat-store';

describe('ChatStore', () => {
  let store: ChatStore;
  let next: (signal: AbortSignal) => AsyncGenerator<ChatEvent>;

  beforeEach(() => {
    sessionStorage.clear();
    // ChatApi'nin yerine sahte bir akış: testte model de ağ da yok
    const fakeApi: Partial<ChatApi> = { stream: (_request, signal) => next(signal) };
    TestBed.configureTestingModule({ providers: [{ provide: ChatApi, useValue: fakeApi }] });
    store = TestBed.inject(ChatStore);
  });

  afterEach(() => vi.useRealTimers());

  function play(steps: Step[]): void {
    next = scripted(steps);
  }

  it('streams deltas into one assistant message and ends in done', async () => {
    play([{ type: 'conversation', id: 'c1' }, { type: 'delta', text: 'Dune ' }, { type: 'delta', text: 'is longer.' }, { type: 'done' }]);
    await store.send('Which is longer?');
    expect(store.status()).toBe('done');
    expect(store.messages().map((m) => [m.role, m.text, m.ending])).toEqual([
      ['user', 'Which is longer?', null],
      ['assistant', 'Dune is longer.', 'complete'],
    ]);
  });

  it('keeps a partial answer and marks it interrupted on an error event', async () => {
    play([{ type: 'delta', text: 'Dune is' }, { type: 'error', message: 'The model stopped responding.', partial: true }]);
    await store.send('Which is longer?');
    expect(store.status()).toBe('error');
    expect(store.error()).toEqual({ kind: 'stream', message: 'The model stopped responding.' });
    expect(store.messages()[1]).toMatchObject({ text: 'Dune is', ending: 'interrupted' });
  });

  it('times out after 30 seconds without a first token while pings keep the connection alive', async () => {
    vi.useFakeTimers();
    play([{ type: 'conversation', id: 'c1' }, ...pings(45_000), { type: 'delta', text: 'too late' }]);
    const done = store.send('Which is longer?');
    await vi.advanceTimersByTimeAsync(29_999);
    expect(store.status()).toBe('submitted');
    await vi.advanceTimersByTimeAsync(1);
    await done;
    expect(store.error()?.kind).toBe('timeout');
    expect(store.messages()[1].text).toBe('');
  });

  it('treats 15 seconds of silence as a lost connection', async () => {
    vi.useFakeTimers();
    play([{ type: 'conversation', id: 'c1' }, { type: 'thinking' }, 60_000, { type: 'done' }]);
    const done = store.send('Which is longer?');
    await vi.advanceTimersByTimeAsync(15_000);
    await done;
    expect(store.status()).toBe('error');
    expect(store.error()?.kind).toBe('network');
  });

  it('treats stop as aborted, not as an error', async () => {
    vi.useFakeTimers();
    play([{ type: 'delta', text: 'Dune' }, 10_000, { type: 'done' }]);
    const done = store.send('Which is longer?');
    await vi.advanceTimersByTimeAsync(100);
    store.stop();
    await done;
    expect(store.status()).toBe('aborted');
    expect(store.error()).toBeNull();
    expect(store.messages()[1]).toMatchObject({ text: 'Dune', ending: 'stopped' });
  });

  it('ignores a second message while the first one is streaming', async () => {
    vi.useFakeTimers();
    play([{ type: 'delta', text: 'Dune' }, 1000, { type: 'done' }]);
    const first = store.send('first');
    await vi.advanceTimersByTimeAsync(10);
    await store.send('second');
    expect(store.messages().filter((m) => m.role === 'user').map((m) => m.text)).toEqual(['first']);
    await vi.advanceTimersByTimeAsync(1000);
    await first;
    expect(store.status()).toBe('done');
  });

  it('counts down Retry-After on 429 before allowing the next message', async () => {
    vi.useFakeTimers();
    next = () => {
      throw new ChatHttpError(429, 'You have sent too many messages.', 12);
    };
    await store.send('Which is longer?');
    expect(store.error()?.kind).toBe('rate-limit');
    expect(store.cooldown()).toBe(12);
    expect(store.canSend()).toBe(false);
    await vi.advanceTimersByTimeAsync(12_000);
    expect(store.cooldown()).toBe(0);
    expect(store.canSend()).toBe(true);
  });
});
