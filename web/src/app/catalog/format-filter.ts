import { Component, model } from '@angular/core';
import { Listbox, Option } from '@angular/aria/listbox';
import { BookFormat } from './book';

@Component({
  selector: 'app-format-filter',
  imports: [Listbox, Option],
  template: `
    <div ngListbox multi selectionMode="explicit" orientation="horizontal" [(value)]="selected" aria-label="Format" class="flex gap-2">
      @for (format of formats; track format) {
        <div ngOption [value]="format" [label]="format"
             class="cursor-pointer rounded-full border border-slate-300 px-3 py-1 text-sm
                    aria-selected:border-brand-600 aria-selected:bg-brand-600 aria-selected:text-white
                    focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-brand-500">
          {{ format }}
        </div>
      }
    </div>
  `,
})
export class FormatFilter {
  protected readonly formats: readonly BookFormat[] = ['Hardcover', 'Paperback', 'E-book'];
  readonly selected = model<BookFormat[]>([]);
}
