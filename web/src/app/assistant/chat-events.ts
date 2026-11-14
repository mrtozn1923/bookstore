// /chat/sse event sözlüğü: Bookstore.Assistant.Api'deki record'larla aynı alanlar
export type ChatEvent =
  | { type: 'conversation'; id: string }
  | { type: 'ping' }
  | { type: 'thinking' }
  | { type: 'delta'; text: string }
  | { type: 'tool'; callId: string; name: string; phase: 'started' | 'completed' }
  | { type: 'usage'; inputTokens: number | null; outputTokens: number | null }
  | { type: 'done' }
  | { type: 'error'; message: string; partial: boolean };

// SSE'den gelen ham event adını ve JSON verisini tiplenmiş ChatEvent'e çevirir
export function toChatEvent(event: string, data: string): ChatEvent | null {
  switch (event) {
    case 'ping':
    case 'thinking':
    case 'done':
      return { type: event };
    case 'conversation':
    case 'delta':
    case 'tool':
    case 'usage':
    case 'error':
      return { type: event, ...JSON.parse(data) };
    default:
      // Bilinmeyen event'i yok say: sunucu yeni bir event eklediğinde eski istemci bozulmasın
      return null;
  }
}
