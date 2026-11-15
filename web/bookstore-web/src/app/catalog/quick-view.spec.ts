import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Component, ElementRef, inject, viewChild } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { QuickView } from './quick-view';

@Component({
  providers: [QuickView],
  template: `<div #card class="card"><button #trigger type="button">Quick look</button></div>`,
})
class Host {
  readonly quickView = inject(QuickView);
  readonly card = viewChild.required<ElementRef<HTMLElement>>('card');
  readonly trigger = viewChild.required<ElementRef<HTMLElement>>('trigger');
}

const book = { isbn: '9780441172719', title: 'Dune', author: 'Frank Herbert', category: 'Science Fiction', price: 489,
  publishedYear: 1965, description: 'Desert planet.', available: 3 };

async function openPanel() {
  TestBed.configureTestingModule({ providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()] });
  const fixture = TestBed.createComponent(Host);
  await fixture.whenStable();
  const host = fixture.componentInstance;
  host.trigger().nativeElement.focus();
  await host.quickView.open(book.isbn, host.card().nativeElement, host.trigger().nativeElement);
  TestBed.tick();
  TestBed.inject(HttpTestingController).expectOne(`/api/books/${book.isbn}`).flush(book);
  await fixture.whenStable();
  return { fixture, host };
}

const pane = () => document.querySelector('.cdk-overlay-pane');

describe('QuickView', () => {
  it('shows the book in an overlay and moves focus into it', async () => {
    await openPanel();
    expect(pane()?.querySelector('h2')?.textContent).toBe('Dune');
    expect(document.activeElement?.getAttribute('role')).toBe('dialog');
  });

  it('closes on Escape and gives focus back to the button', async () => {
    const { host } = await openPanel();
    document.body.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));
    expect(pane()).toBeNull();
    expect(host.quickView.openIsbn()).toBeNull();
    expect(document.activeElement).toBe(host.trigger().nativeElement);
  });

  it('removes the overlay when the page that opened it is destroyed', async () => {
    const { fixture } = await openPanel();
    fixture.destroy();
    expect(pane()).toBeNull();
  });
});
