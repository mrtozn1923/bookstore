import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { form, required } from '@angular/forms/signals';
import { FieldErrorsComponent } from './field-errors.component';

@Component({
  imports: [FieldErrorsComponent],
  template: `<app-field-errors [field]="titleForm" />`
})
class Host {
  readonly titleForm = form(signal(''), (title) => required(title, { message: 'Title is required' }));
}

describe('FieldErrorsComponent', () => {
  it('renders the messages of a touched field', async () => {
    const fixture = TestBed.createComponent(Host);
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent.trim()).toBe('');
    fixture.componentInstance.titleForm().markAsTouched();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent.trim()).toBe('Title is required');
  });
});
