import { readSse, SseMessage } from './sse';

// Verilen parçaları ayrı chunk'lar olarak veren bir ReadableStream: ağdaki bölünmeyi taklit eder
function streamOf(chunks: (string | Uint8Array<ArrayBuffer>)[]): ReadableStream<Uint8Array<ArrayBuffer>> {
  const encoder = new TextEncoder();
  return new ReadableStream({
    start(controller) {
      for (const c of chunks) {
        controller.enqueue(typeof c === 'string' ? encoder.encode(c) : c);
      }
      controller.close();
    },
  });
}

async function collect(stream: ReadableStream<Uint8Array<ArrayBuffer>>): Promise<SseMessage[]> {
  const result: SseMessage[] = [];
  for await (const message of readSse(stream)) {
    result.push(message);
  }
  return result;
}

describe('readSse', () => {
  it('joins a UTF-8 character split across two chunks', async () => {
    const bytes = new TextEncoder().encode('event: delta\ndata: {"text":"Doğu"}\n\n');
    const at = bytes.indexOf(0xc4); // "ğ" = C4 9F
    const messages = await collect(streamOf([bytes.slice(0, at + 1), bytes.slice(at + 1)]));
    expect(messages).toEqual([{ event: 'delta', data: '{"text":"Doğu"}' }]);
  });

  it('does not invent an empty line when CRLF is split between chunks', async () => {
    const messages = await collect(streamOf(['event: delta\r', '\ndata: {"text":"a"}\r\n\r\n']));
    expect(messages).toEqual([{ event: 'delta', data: '{"text":"a"}' }]);
  });

  it('skips comments, joins multi-line data and drops an unterminated last event', async () => {
    const messages = await collect(
      streamOf([': keep-alive\n\nevent: note\ndata: line 1\ndata: line 2\n\n', 'event: delta\ndata: {"text":"lost"}']),
    );
    expect(messages).toEqual([{ event: 'note', data: 'line 1\nline 2' }]);
  });
});
