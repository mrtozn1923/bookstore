import { TestBed } from '@angular/core/testing';
import { App } from './app';

describe('App', () => {
  it('renders one card per book', async () => {
    await TestBed.configureTestingModule({ imports: [App] }).compileComponents();
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();

    const cards = (fixture.nativeElement as HTMLElement).querySelectorAll('app-book-card');
    expect(cards.length).toBe(2);
  });
});
