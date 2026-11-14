import { Component, signal } from '@angular/core';
import { HighlightDirective } from '../highlight.directive';
import { OrderTrackingComponent } from '../order-tracking/order-tracking.component';
import { NgClass, NgStyle } from '@angular/common';

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
    imports: [HighlightDirective, NgClass, NgStyle, OrderTrackingComponent]
})
export class OrderListComponent {
  isLoading = false;
  showTracking = false;
  trackedId = signal(1042);
  orders: Order[] = [
    { id: 1040, books: 'The Hobbit', status: 'delivered', shipping: 'standard' },
    { id: 1041, books: 'Middlemarch', status: 'shipped', shipping: 'express' },
    { id: 1042, books: 'Dune, Emma', status: 'pending', shipping: 'standard' }
  ];
}
