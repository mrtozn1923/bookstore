// Splits a stream of text chunks into lines; a line can be cut between two chunks
export function splitLines(): TransformStream<string, string> {
  let rest = '';
  return new TransformStream({
    transform(chunk, controller) {
      const lines = (rest + chunk).split('\n');
      rest = lines.pop() ?? '';
      for (const line of lines) controller.enqueue(line);
    },
    flush(controller) {
      if (rest) controller.enqueue(rest);
    },
  });
}
