import { Component, DOCUMENT, effect, inject, signal } from '@angular/core';
import { MatButton } from '@angular/material/button';

type Theme = 'system' | 'light' | 'dark';
const NEXT: Record<Theme, Theme> = { system: 'light', light: 'dark', dark: 'system' };

@Component({
  selector: 'app-theme-toggle',
  imports: [MatButton],
  template: `<button matButton="outlined" (click)="toggle()">Theme: {{ theme() }}</button>`,
})
export class ThemeToggle {
  private readonly root = inject(DOCUMENT).documentElement;
  protected readonly theme = signal<Theme>('system');

  constructor() {
    // only a class changes at runtime; every color is already in the CSS
    effect(() => {
      this.root.classList.remove('light', 'dark');
      const theme = this.theme();
      if (theme !== 'system') {
        this.root.classList.add(theme);
      }
    });
  }

  protected toggle(): void {
    this.theme.update((current) => NEXT[current]);
  }
}
