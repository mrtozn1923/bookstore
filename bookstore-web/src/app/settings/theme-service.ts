import { Service, signal } from '@angular/core';

export type Theme = 'light' | 'dark';

// A small, synchronous preference: localStorage is the right place for it
@Service()
export class ThemeService {
  readonly theme = signal<Theme>(this.read());

  set(theme: Theme) {
    this.theme.set(theme);
    try {
      localStorage.setItem('theme', theme);
    } catch (err) {
      // storage can be full, disabled or blocked; the app must keep working without it
      console.warn('theme not saved', err);
    }
  }

  private read(): Theme {
    try {
      return localStorage.getItem('theme') === 'dark' ? 'dark' : 'light';
    } catch {
      return 'light';
    }
  }
}
