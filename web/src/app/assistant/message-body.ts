import { Component, computed, input } from '@angular/core';
import { Source } from './chat-events';
import { CodeBlock } from './code-block';
import { StreamingMarkdown } from './markdown';

// [n] atıflarını, kaynak listesinde varsa o kaynağın sayfasına link yapar; olmayan numara düz metin kalır
export function linkCitations(text: string, sources: Source[]): string {
  return text.replace(/\[(\d+)\](?!\()/g, (match, n: string) => {
    const source = sources.find((s) => s.id === Number(n));
    return source ? `[[${n}]](${source.url})` : match;
  });
}

@Component({
  selector: 'app-message-body',
  imports: [CodeBlock],
  template: `
    @for (block of blocks(); track $index) {
      @if (block.kind === 'code') {
        <app-code-block [code]="block.code" [lang]="block.lang" />
      } @else {
        <div class="md" [innerHTML]="block.html"></div>
      }
    }
    @if (cited().length > 0) {
      <p class="sources">
        Sources:
        @for (source of cited(); track source.id) {
          <a [href]="source.url">[{{ source.id }}] {{ source.title }}</a>
        }
      </p>
    }
  `,
  styleUrl: './message-body.css',
})
export class MessageBody {
  readonly text = input.required<string>();
  readonly final = input(false);
  readonly sources = input<Source[]>([]);

  // Her message-body kendi renderer'ını tutar: tamamlanan bloklar bir kez işlenir
  private readonly renderer = new StreamingMarkdown();
  protected readonly blocks = computed(() =>
    this.renderer.update(linkCitations(this.text(), this.sources()), this.final()),
  );
  // Yalnız metinde gerçekten atıf yapılan kaynakları göster
  protected readonly cited = computed(() =>
    this.sources().filter((s) => this.text().includes(`[${s.id}]`)),
  );
}
