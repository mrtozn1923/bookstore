import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { LegacyQuantityStepperComponent } from './legacy-quantity-stepper.component';

@Component({
  imports: [LegacyQuantityStepperComponent, ReactiveFormsModule],
  template: `<app-legacy-quantity-stepper [formControl]="quantity" />`
})
class Host {
  readonly quantity = new FormControl(3, { nonNullable: true });
}

describe('LegacyQuantityStepperComponent', () => {
  it('talks to a FormControl through ControlValueAccessor', () => {
    const fixture = TestBed.createComponent(Host);
    fixture.detectChanges();
    const control = fixture.componentInstance.quantity;
    const [minus, plus] = fixture.nativeElement.querySelectorAll('button');
    expect(fixture.nativeElement.querySelector('span').textContent).toBe('3');
    plus.click();
    expect(control.value).toBe(4);
    expect(control.dirty).toBe(true);
    plus.dispatchEvent(new Event('blur'));
    expect(control.touched).toBe(true);
    control.setValue(7);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('span').textContent).toBe('7');
    control.disable();
    fixture.detectChanges();
    expect(minus.disabled).toBe(true);
  });
});
