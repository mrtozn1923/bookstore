import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { FormField, disabled, form } from '@angular/forms/signals';
import { GiftWrapToggleComponent } from './gift-wrap-toggle.component';

@Component({
  imports: [GiftWrapToggleComponent, FormField],
  template: `<app-gift-wrap-toggle [formField]="optionsForm.giftWrap" />`
})
class Host {
  readonly pickup = signal(false);
  readonly options = signal({ giftWrap: false });
  readonly optionsForm = form(this.options, (o) => disabled(o.giftWrap, { when: () => this.pickup() }));
}

describe('GiftWrapToggleComponent', () => {
  it('binds checked and disabled through [formField]', async () => {
    const fixture = TestBed.createComponent(Host);
    fixture.autoDetectChanges();
    await fixture.whenStable();
    const button: HTMLButtonElement = fixture.nativeElement.querySelector('button');
    expect(button.getAttribute('aria-checked')).toBe('false');
    button.click();
    await fixture.whenStable();
    expect(fixture.componentInstance.options()).toEqual({ giftWrap: true });
    expect(button.textContent!.trim()).toBe('Gift wrap: on');
    fixture.componentInstance.pickup.set(true);
    await fixture.whenStable();
    expect(button.disabled).toBe(true);
  });
});
