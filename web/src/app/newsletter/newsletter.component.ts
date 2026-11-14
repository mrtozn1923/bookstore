import { Component } from '@angular/core';
import { UntypedFormControl } from '@angular/forms';

@Component({
  selector: 'app-newsletter',
  template: `
    <input type="email" [formControl]="email">
    <p>Newsletter will be sent to: {{ email.value }}</p>
  `
})
export class NewsletterComponent {
  email = new UntypedFormControl('');
}
