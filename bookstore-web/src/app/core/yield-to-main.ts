// scheduler.yield(): Chrome 129+, Firefox 142+; Safari has no Scheduler API yet -> setTimeout fallback.
// The continuation of scheduler.yield() runs before other queued tasks, setTimeout's at the back.
export function yieldToMain(): Promise<void> {
  const scheduler = (globalThis as { scheduler?: { yield?: () => Promise<void> } }).scheduler;
  if (scheduler?.yield) {
    return scheduler.yield();
  }
  return new Promise((resolve) => setTimeout(resolve, 0));
}
