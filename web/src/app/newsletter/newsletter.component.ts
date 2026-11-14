import { Component } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule, StatusChangeEvent, TouchedChangeEvent, ValueChangeEvent, Validators } from '@angular/forms';

@Component({
    selector: 'app-newsletter',
    template: `
    <input type="email" [formControl]="email">
    <p>Newsletter will be sent to: {{ email.value }}</p>
  `,
    imports: [ReactiveFormsModule]
})
export class NewsletterComponent {
  email = new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.email] });

  constructor() {
    this.email.events.pipe(takeUntilDestroyed()).subscribe(event => {
      if (event instanceof ValueChangeEvent) {
        console.log('Value:', event.value);
      }
      if (event instanceof StatusChangeEvent) {
        console.log('Status:', event.status);
      }
      if (event instanceof TouchedChangeEvent) {
        console.log('Touched:', event.touched);
      }
    });
  }
}
