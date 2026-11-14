export interface SseMessage {
  event: string;
  data: string;
}

// WHATWG event stream formatı: satır sonu CRLF, LF ya da CR olabilir; boş satır event'i gönderir
export async function* readSse(body: ReadableStream<BufferSource>): AsyncGenerator<SseMessage> {
  // TextDecoderStream, chunk sınırında bölünen çok baytlı UTF-8 karakterini bir sonraki chunk'la birleştirir
  const reader = body.pipeThrough(new TextDecoderStream()).getReader();
  let buffer = '';
  let event = '';
  let data: string[] = [];
  try {
    while (true) {
      const { value, done } = await reader.read();
      if (done) {
        // Boş satırla kapanmamış son event spec gereği atılır
        return;
      }
      buffer += value;
      // CRLF'in \r'si chunk sonunda kaldıysa \n'yi bekle; yoksa araya sahte bir boş satır girer
      const cut = buffer.endsWith('\r') ? buffer.length - 1 : buffer.length;
      const lines = buffer.slice(0, cut).split(/\r\n|\r|\n/);
      buffer = lines.pop()! + buffer.slice(cut);
      for (const line of lines) {
        if (line === '') {
          if (data.length > 0) {
            yield { event: event || 'message', data: data.join('\n') };
          }
          event = '';
          data = [];
          continue;
        }
        if (line.startsWith(':')) {
          continue; // yorum satırı
        }
        const colon = line.indexOf(':');
        const field = colon === -1 ? line : line.slice(0, colon);
        let fieldValue = colon === -1 ? '' : line.slice(colon + 1);
        if (fieldValue.startsWith(' ')) {
          fieldValue = fieldValue.slice(1);
        }
        if (field === 'event') {
          event = fieldValue;
        } else if (field === 'data') {
          data.push(fieldValue);
        }
      }
    }
  } finally {
    // Tüketici döngüden erken çıkarsa (break, abort) bağlantıyı da kapat
    await reader.cancel().catch(() => {});
  }
}
