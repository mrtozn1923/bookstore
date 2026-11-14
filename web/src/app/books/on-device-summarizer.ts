// Chrome'un built-in Summarizer API'si TypeScript'in DOM tiplerinde yok; kullandığımız kadarını tanımlıyoruz
export type Availability = 'unavailable' | 'downloadable' | 'downloading' | 'available';

interface SummarizerOptions {
  type: 'tldr' | 'key-points' | 'teaser' | 'headline';
  format: 'plain-text' | 'markdown';
  length: 'short' | 'medium' | 'long';
  sharedContext?: string;
  monitor?: (m: EventTarget) => void;
}

interface SummarizerSession {
  summarize(text: string, options?: { signal?: AbortSignal }): Promise<string>;
  destroy(): void;
}

interface SummarizerApi {
  availability(options?: Partial<SummarizerOptions>): Promise<Availability>;
  create(options: SummarizerOptions): Promise<SummarizerSession>;
}

const OPTIONS: SummarizerOptions = {
  type: 'tldr',
  format: 'plain-text',
  length: 'short',
  sharedContext: 'Customer reviews of a book in an online bookstore.',
};

function api(): SummarizerApi | undefined {
  return (globalThis as { Summarizer?: SummarizerApi }).Summarizer;
}

// Özellik kontrolü: API yoksa ya da availability() 3 saniyede dönmezse cihazda özet yok sayılır
export async function onDeviceAvailability(): Promise<Availability> {
  const summarizer = api();
  if (!summarizer) {
    return 'unavailable';
  }
  const timeout = new Promise<Availability>((resolve) => setTimeout(() => resolve('unavailable'), 3000));
  return Promise.race([summarizer.availability(OPTIONS), timeout]).catch(() => 'unavailable' as const);
}

// Model gerekiyorsa indirir (kullanıcı etkileşimi şart), ilerlemeyi 0..1 olarak bildirir ve özetler
export async function summarizeOnDevice(text: string, onProgress: (loaded: number) => void, signal?: AbortSignal): Promise<string> {
  const summarizer = api();
  if (!summarizer) {
    throw new Error('Summarizer API is not available.');
  }
  const session = await summarizer.create({
    ...OPTIONS,
    monitor: (m) => m.addEventListener('downloadprogress', (e) => onProgress((e as ProgressEvent).loaded)),
  });
  try {
    return await session.summarize(text, { signal });
  } finally {
    session.destroy();
  }
}
