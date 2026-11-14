import { Component, Input, OnInit } from '@angular/core';

@Component({
  selector: 'app-order-tracking',
  template: `<p>{{ statusText }}</p>`
})
export class OrderTrackingComponent implements OnInit {
  @Input() orderId = 0;
  statusText = '';

  constructor() {
    console.log('constructor, orderId:', this.orderId);
  }

  ngOnInit(): void {
    console.log('ngOnInit, orderId:', this.orderId);
    this.statusText = `Loading order #${this.orderId}...`;
  }
}
