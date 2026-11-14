import { Component } from '@angular/core';
import { BookCard } from './book-card';

@Component({
  imports: [BookCard],
  selector: 'app-root',
  template: `
    <div class="shelf">
      <aside>
        <app-book-card bookTitle="Emma" author="Jane Austen" cover="emma.svg" />
      </aside>
      <main>
        <app-book-card bookTitle="Dune" author="Frank Herbert" cover="dune.svg" [note]="duneNote" />
      </main>
    </div>
  `,
  styles: `
    .shelf {
      display: grid;
      grid-template-columns: 14rem minmax(0, 1fr);
      gap: 1rem;
    }
  `,
})
export class App {
  // in the real app this HTML comes from the CMS
  protected readonly duneNote = '<p>Read the <a href="guide.html">reading guide</a> first.</p>';
}
