import { Component } from '@angular/core';
import { UntypedFormControl, FormsModule, ReactiveFormsModule } from '@angular/forms';

@Component({
    selector: 'app-newsletter',
    template: `
    <input type="email" [formControl]="email">
    <p>Newsletter will be sent to: {{ email.value }}</p>
  `,
    standalone: true,
    imports: [FormsModule, ReactiveFormsModule]
})
export class NewsletterComponent {
  email = new UntypedFormControl('');
}
