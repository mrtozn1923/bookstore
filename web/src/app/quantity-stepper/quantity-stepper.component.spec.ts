import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { FormControl, FormGroup, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { FormField, form, min } from '@angular/forms/signals';
import { QuantityStepperComponent } from './quantity-stepper.component';

@Component({
  imports: [QuantityStepperComponent, FormField],
  template: `<app-quantity-stepper [formField]="lineForm.quantity" /><button id="next">Next</button>`
})
class SignalHost {
  readonly line = signal({ quantity: 1 });
  readonly lineForm = form(this.line, (l) => min(l.quantity, 1));
}

@Component({
  imports: [QuantityStepperComponent, ReactiveFormsModule],
  template: `<form [formGroup]="group"><app-quantity-stepper formControlName="quantity" /></form>`
})
class ReactiveHost {
  readonly group = new FormGroup({ quantity: new FormControl(3, { nonNullable: true }) });
}

@Component({
  imports: [QuantityStepperComponent, FormsModule],
  template: `<app-quantity-stepper [(ngModel)]="quantity" />`
})
class TemplateHost {
  quantity = 2;
}

const buttons = (el: HTMLElement) => el.querySelectorAll<HTMLButtonElement>('app-quantity-stepper button');
const shown = (el: HTMLElement) => el.querySelector<HTMLInputElement>('app-quantity-stepper input')!.value;

describe('QuantityStepperComponent', () => {
  it('works with [formField]', async () => {
    const fixture = TestBed.createComponent(SignalHost);
    fixture.autoDetectChanges();
    await fixture.whenStable();
    const el: HTMLElement = fixture.nativeElement;
    expect(buttons(el)[0].disabled).toBe(true);
    buttons(el)[1].click();
    await fixture.whenStable();
    expect(fixture.componentInstance.line()).toEqual({ quantity: 2 });
    expect(buttons(el)[0].disabled).toBe(false);
    expect(fixture.componentInstance.lineForm.quantity().touched()).toBe(false);
    buttons(el)[1].dispatchEvent(new FocusEvent('focusout', { bubbles: true, relatedTarget: el.querySelector('#next') }));
    expect(fixture.componentInstance.lineForm.quantity().touched()).toBe(true);
  });

  it('works with formControlName', async () => {
    const fixture = TestBed.createComponent(ReactiveHost);
    fixture.detectChanges();
    await fixture.whenStable();
    const el: HTMLElement = fixture.nativeElement;
    const control = fixture.componentInstance.group.controls.quantity;
    expect(shown(el)).toBe('3');
    buttons(el)[1].click();
    expect(control.value).toBe(4);
    buttons(el)[1].dispatchEvent(new FocusEvent('focusout', { bubbles: true }));
    expect(control.touched).toBe(true);
    control.disable();
    fixture.detectChanges();
    expect(buttons(el)[1].disabled).toBe(true);
  });

  it('works with ngModel', async () => {
    const fixture = TestBed.createComponent(TemplateHost);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    const el: HTMLElement = fixture.nativeElement;
    expect(shown(el)).toBe('2');
    buttons(el)[0].click();
    expect(fixture.componentInstance.quantity).toBe(1);
  });
});
