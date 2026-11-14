// Draws marble diagrams from real runs: the source marbles are played on RxJS's TestScheduler
// (virtual time, 1 character = 1 frame), the result is recorded and rendered as a marble string,
// and that string is then checked with expectObservable, so every printed line is verified by RxJS.
import { deepStrictEqual } from "node:assert";
import { inspect } from "node:util";
import type { Observable } from "rxjs";
import { TestScheduler } from "rxjs/testing";

type Cold = <V>(marbles: string, values?: Record<string, V>, error?: unknown) => Observable<V>;
type Event = { frame: number; kind: "N" | "E" | "C"; value?: unknown };

const show = (v: unknown) => (typeof v === "string" ? v : inspect(v, { breakLength: Infinity }));

function render(events: Event[]): { marbles: string; values: Record<string, unknown>; legend: string } {
  const letters = "abcdefghijklmnopqrstuvwxyz";
  const values: Record<string, unknown> = {};
  const keyOf = (v: unknown) => {
    const known = Object.entries(values).find(([, x]) => Object.is(x, v)); // same reference = same letter
    if (known) return known[0];
    const key = letters[Object.keys(values).length];
    values[key] = v;
    return key;
  };
  let marbles = "";
  for (const frame of [...new Set(events.map((e) => e.frame))]) {
    if (frame < marbles.length) throw new Error(`cannot draw frame ${frame} after ${marbles}`);
    marbles = marbles.padEnd(frame, "-");
    const chars = events.filter((e) => e.frame === frame).map((e) => (e.kind === "N" ? keyOf(e.value) : e.kind === "E" ? "#" : "|"));
    marbles += chars.length === 1 ? chars[0] : `(${chars.join("")})`;
  }
  const legend = Object.entries(values).map(([k, v]) => `${k}=${show(v)}`).join(" ");
  return { marbles, values, legend };
}

export function showMarbles(title: string, build: (cold: Cold) => Observable<unknown>, totalFrames = 20): void {
  const inputs: { marbles: string; legend: string }[] = [];
  const events: Event[] = [];
  let lastError: unknown;
  const recorder = new TestScheduler(() => {});
  recorder.run(({ cold }) => {
    const recordingCold: Cold = (marbles, values, error) => {
      const legend = values ? Object.entries(values).filter(([k]) => marbles.includes(k)).map(([k, v]) => `${k}=${show(v)}`).join(" ") : "";
      inputs.push({ marbles, legend });
      return cold(marbles, values, error);
    };
    const sub = build(recordingCold).subscribe({
      next: (value) => events.push({ frame: recorder.now(), kind: "N", value }),
      error: (err) => { lastError = err; events.push({ frame: recorder.now(), kind: "E" }); },
      complete: () => events.push({ frame: recorder.now(), kind: "C" }),
    });
    recorder.schedule(() => sub.unsubscribe(), totalFrames);
  });
  const out = render(events);

  // second run: let RxJS itself confirm the rendered marble
  const checker = new TestScheduler((actual, expected) => deepStrictEqual(actual, expected));
  checker.run(({ cold, expectObservable }) => {
    expectObservable(build(cold as Cold), "^" + "-".repeat(totalFrames - 1) + "!").toBe(out.marbles, out.values, lastError);
  });

  console.log(title);
  for (const input of inputs) console.log(`  in : ${input.marbles.padEnd(16)} ${input.legend}`);
  const error = lastError instanceof Error ? ` #=${lastError.name}` : "";
  console.log(`  out: ${out.marbles.padEnd(16)} ${out.legend}${error}`);
}
