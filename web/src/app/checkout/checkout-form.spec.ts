import { TestBed } from '@angular/core/testing';
import { render, screen } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';
import { CheckoutForm } from './checkout-form';

describe('CheckoutForm', () => {
  it('shows an error after the user leaves an invalid email', async () => {
    await render(CheckoutForm);
    const user = userEvent.setup();

    await user.type(screen.getByLabelText('Email'), 'ada.example.com');
    await user.tab();

    const emailInput = screen.getByRole('textbox', { name: 'Email' });
    expect(emailInput).toHaveAccessibleDescription('Enter a valid email address.');
    expect(emailInput).toBeInvalid();
  });

  it('shows every missing field when an empty form is submitted', async () => {
    const placeOrder = vi.fn();
    await render(CheckoutForm, { on: { placeOrder } });
    const user = userEvent.setup();

    await user.click(screen.getByRole('button', { name: 'Place order' }));

    expect(screen.getByRole('textbox', { name: 'Full name' })).toHaveAccessibleDescription('Enter your full name.');
    expect(screen.getByRole('textbox', { name: 'Email' })).toHaveAccessibleDescription('Enter your email address.');
    expect(screen.getByRole('combobox', { name: 'City' })).toHaveAccessibleDescription('Choose a city.');
    expect(placeOrder).not.toHaveBeenCalled();
  });

  it('places the order with the details the user entered', async () => {
    const placeOrder = vi.fn();
    await render(CheckoutForm, { on: { placeOrder } });
    const user = userEvent.setup();

    await user.type(screen.getByLabelText('Full name'), 'Ada Lovelace');
    await user.type(screen.getByLabelText('Email'), 'ada@example.com');
    await user.selectOptions(screen.getByLabelText('City'), 'Izmir');
    await user.click(screen.getByRole('button', { name: 'Place order' }));

    expect(placeOrder).toHaveBeenCalledExactlyOnceWith({ fullName: 'Ada Lovelace', email: 'ada@example.com', city: 'Izmir' });
  });
});

describe('CheckoutForm with TestBed only', () => {
  it('shows an error after the user leaves an invalid email', async () => {
    const fixture = TestBed.createComponent(CheckoutForm);
    await fixture.whenStable();
    const input: HTMLInputElement = fixture.nativeElement.querySelector('#email');

    input.value = 'ada.example.com';
    input.dispatchEvent(new Event('input'));
    input.dispatchEvent(new Event('blur'));
    await fixture.whenStable();

    expect(fixture.nativeElement.querySelector('#email-error').textContent).toBe('Enter a valid email address.');
    expect(input.getAttribute('aria-describedby')).toBe('email-error');
  });
});
