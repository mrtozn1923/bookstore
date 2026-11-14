import { Component } from '@angular/core';
import { HighlightDirective } from '../highlight.directive';
import { NgIf, NgFor, NgClass, NgStyle, NgSwitch, NgSwitchCase, NgSwitchDefault } from '@angular/common';

export interface Order {
  id: number;
  books: string;
  status: 'pending' | 'shipped' | 'delivered';
  shipping: 'standard' | 'express';
}

@Component({
    selector: 'app-order-list',
    templateUrl: './order-list.component.html',
    styleUrls: ['./order-list.component.css'],
    standalone: true,
    imports: [NgIf, NgFor, HighlightDirective, NgClass, NgStyle, NgSwitch, NgSwitchCase, NgSwitchDefault]
})
export class OrderListComponent {
  isLoading = false;
  orders: Order[] = [
    { id: 1040, books: 'The Hobbit', status: 'delivered', shipping: 'standard' },
    { id: 1041, books: 'Middlemarch', status: 'shipped', shipping: 'express' },
    { id: 1042, books: 'Dune, Emma', status: 'pending', shipping: 'standard' }
  ];

  trackById(index: number, order: Order): number {
    return order.id;
  }
}
