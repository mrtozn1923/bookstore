// Splits a stream of text chunks into lines; a line can be cut between two chunks
export function splitLines() {
  let rest = "";
  return new TransformStream({
    transform(chunk, controller) {
      const lines = (rest + chunk).split("\n");
      rest = lines.pop();
      for (const line of lines) controller.enqueue(line);
    },
    flush(controller) {
      if (rest) controller.enqueue(rest);
    },
  });
}

export async function sumReport(url = "/api/reports/orders.csv?rows=20000") {
  const started = performance.now();
  const response = await fetch(url);
  if (!response.ok) throw new Error(`HTTP ${response.status}`);

  let rows = 0;
  let total = 0;
  let firstRowMs = 0;
  const lines = response.body.pipeThrough(new TextDecoderStream()).pipeThrough(splitLines());
  for await (const line of lines) {
    if (line.startsWith("orderId")) continue; // header
    if (rows++ === 0) firstRowMs = performance.now() - started;
    total += Number(line.split(",")[4]);
  }
  const ms = performance.now() - started;
  console.log(`stream: first row after ${firstRowMs.toFixed(0)} ms, ${rows} rows, ${total} TRY in ${ms.toFixed(0)} ms`);
}

export async function sumReportWithText(url = "/api/reports/orders.csv?rows=20000") {
  const started = performance.now();
  const response = await fetch(url);
  const text = await response.text(); // waits for the whole body
  const firstRowMs = performance.now() - started;
  const rows = text.trim().split("\n").slice(1);
  const total = rows.reduce((sum, line) => sum + Number(line.split(",")[4]), 0);
  const ms = performance.now() - started;
  console.log(`text(): first row after ${firstRowMs.toFixed(0)} ms, ${rows.length} rows, ${total} TRY in ${ms.toFixed(0)} ms`);
}
