import { Component, ViewEncapsulation, input, output } from '@angular/core';

// Tasarım sistemindeki dosya seçici gibi kendi shadow root'unda render edilir
@Component({
  selector: 'app-file-picker',
  encapsulation: ViewEncapsulation.ShadowDom,
  template: `
    <div class="picker">
      <label for="file">{{ label() }}</label>
      <input id="file" type="file" [accept]="accept()" (change)="onChange($event)" />
      <p class="hint">{{ hint() }}</p>
    </div>
  `,
  styles: `
    .picker { border: 2px dashed #94a3b8; border-radius: 12px; padding: 16px; background: #fff; }
    label { display: block; font-weight: 600; margin-bottom: 8px; }
    .hint { color: #475569; font-size: 14px; margin: 8px 0 0; }
  `,
})
export class FilePicker {
  readonly label = input.required<string>();
  readonly accept = input('');
  readonly hint = input('');
  readonly picked = output<File>();

  protected onChange(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (file) {
      this.picked.emit(file);
    }
  }
}
