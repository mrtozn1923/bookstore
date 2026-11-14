import { Component, computed, input, signal } from '@angular/core';
import hljs from 'highlight.js/lib/core';
import csharp from 'highlight.js/lib/languages/csharp';
import json from 'highlight.js/lib/languages/json';
import typescript from 'highlight.js/lib/languages/typescript';

hljs.registerLanguage('csharp', csharp);
hljs.registerLanguage('json', json);
hljs.registerLanguage('typescript', typescript);

@Component({
  selector: 'app-code-block',
  template: `
    <div class="code-header">
      <span>{{ lang() || 'text' }}</span>
      <button type="button" (click)="copy()">{{ copied() ? 'Copied' : 'Copy' }}</button>
    </div>
    @if (highlighted(); as html) {
      <pre><code [innerHTML]="html"></code></pre>
    } @else {
      <pre><code>{{ code() }}</code></pre>
    }
  `,
  styleUrl: './code-block.css',
})
export class CodeBlock {
  readonly code = input.required<string>();
  readonly lang = input('');
  protected readonly copied = signal(false);

  // highlight.js kodu kendisi escape eder; çıktı yalnız span ve class içerir. Bilinmeyen dil düz metin kalır.
  protected readonly highlighted = computed(() =>
    hljs.getLanguage(this.lang()) ? hljs.highlight(this.code(), { language: this.lang() }).value : null,
  );

  protected async copy(): Promise<void> {
    // Panoya HTML değil, ham kod kopyalanır
    await navigator.clipboard.writeText(this.code());
    this.copied.set(true);
    setTimeout(() => this.copied.set(false), 1500);
  }
}
