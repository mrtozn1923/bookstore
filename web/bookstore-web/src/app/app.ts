import { Component, ElementRef, OnInit, inject, viewChild } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';
import { CartStore } from './cart/cart-store';

@Component({
  imports: [RouterOutlet, RouterLink],
  selector: 'app-root',
  styleUrl: './app.css',
  templateUrl: './app.html',
})
export class App implements OnInit {
  protected readonly cartStore = inject(CartStore);
  private readonly main = viewChild.required<ElementRef<HTMLElement>>('main');

  ngOnInit(): void {
    void this.cartStore.load();
  }

  // With <base href="/">, href="#main-content" would navigate to "/#main-content"; move focus ourselves.
  protected skipToMain(event: Event): void {
    event.preventDefault();
    this.main().nativeElement.focus();
  }
}
