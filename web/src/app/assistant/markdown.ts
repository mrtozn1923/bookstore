import DOMPurify from 'dompurify';
import { Marked, type Token, type TokensList } from 'marked';

export type Block =
  | { kind: 'code'; code: string; lang: string }
  | { kind: 'html'; html: string };

const marked = new Marked({ gfm: true });

// Model çıktısındaki link'ler için izinli hedefler: https, mailto, site içi yol ve sayfa içi çapa
const SAFE_HREF = /^(https:\/\/|mailto:|\/(?!\/)|#)/i;

// Yarım bloktaki link'ler tıklanamaz: "https://exa" gibi yarım bir URL'e gitmesin
let inertLinks = false;

DOMPurify.addHook('afterSanitizeAttributes', (node) => {
  if (node.tagName !== 'A') {
    return;
  }
  const href = node.getAttribute('href') ?? '';
  if (inertLinks || !SAFE_HREF.test(href)) {
    node.removeAttribute('href');
  } else if (href.startsWith('https://')) {
    node.setAttribute('target', '_blank');
    node.setAttribute('rel', 'noopener noreferrer nofollow');
  }
});

// Markdown → HTML → DOMPurify; görseller tamamen yasak (URL ile veri sızdırma riski)
export function sanitizeMarkdown(tokens: Token[], links: TokensList['links'], inert = false): string {
  const html = marked.parser(Object.assign(tokens, { links }));
  inertLinks = inert;
  try {
    return DOMPurify.sanitize(html, {
      FORBID_TAGS: ['img', 'picture', 'svg', 'math', 'form', 'input', 'button', 'style'],
      FORBID_ATTR: ['style', 'id', 'class'],
    });
  } finally {
    inertLinks = false;
  }
}

// Boşluk token'ı ve sanitize sonrası boş kalan blok ekrana gelmez
function toBlock(token: Token, links: TokensList['links'], inert = false): Block | null {
  if (token.type === 'space') {
    return null;
  }
  if (token.type === 'code') {
    return { kind: 'code', code: token.text, lang: token.lang ?? '' };
  }
  const html = sanitizeMarkdown([token], links, inert);
  return html.replace(/<p><\/p>/g, '').trim() === '' ? null : { kind: 'html', html };
}

// Streaming markdown: tamamlanan blokları dondurur, her güncellemede yalnız son bloğu yeniden işler
export class StreamingMarkdown {
  private readonly frozen: Block[] = [];
  private offset = 0;

  update(text: string, final: boolean): Block[] {
    const tokens = marked.lexer(text.slice(this.offset));
    // Bir blok, arkasından boşluk olmayan yeni bir blok başladıysa tamamlanmıştır.
    // Boş satır yetmez: "- a\n\n- b" tek bir listedir.
    let complete = tokens.length;
    if (!final) {
      complete = tokens.length - 1;
      while (complete >= 0 && tokens[complete].type === 'space') {
        complete--;
      }
    }
    for (let i = 0; i < complete; i++) {
      const block = toBlock(tokens[i], tokens.links);
      if (block) {
        this.frozen.push(block);
      }
      this.offset += tokens[i].raw.length;
    }
    const tail = final || complete < 0 ? null : toBlock(tokens[complete], tokens.links, true);
    return tail ? [...this.frozen, tail] : [...this.frozen];
  }
}
