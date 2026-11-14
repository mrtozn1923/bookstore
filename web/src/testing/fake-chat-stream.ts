import { ChatEvent } from '../app/assistant/chat-events';

// Sahte akışın adımları: bir event ya da milisaniye cinsinden bekleme
export type Step = ChatEvent | number;

// Modeli devreden çıkarır: adımları sırayla verir, beklemeleri setTimeout ile yapar (fake timer'la ilerletilir)
// ve gerçek fetch gibi abort edilince signal.reason ile reject eder
export function scripted(steps: Step[]): (signal: AbortSignal) => AsyncGenerator<ChatEvent> {
  return async function* (signal: AbortSignal) {
    for (const step of steps) {
      if (typeof step === 'number') {
        await new Promise<void>((resolve, reject) => {
          const timer = setTimeout(resolve, step);
          signal.addEventListener('abort', () => {
            clearTimeout(timer);
            reject(signal.reason);
          }, { once: true });
        });
      } else {
        signal.throwIfAborted();
        yield step;
      }
    }
  };
}

// Sunucu gibi her 5 saniyede bir ping gönderen bekleme
export function pings(totalMs: number): Step[] {
  const steps: Step[] = [];
  for (let t = 5000; t <= totalMs; t += 5000) {
    steps.push(5000, { type: 'ping' });
  }
  return steps;
}
