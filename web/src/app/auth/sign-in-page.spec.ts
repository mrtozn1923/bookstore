import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { render, screen } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';
import { useMockApi } from '../../testing/msw';
import { Session } from './session';
import { SignInPage } from './sign-in-page';

describe('SignInPage', () => {
  useMockApi();

  beforeEach(() => localStorage.clear());

  it('signs the user in and goes back to the page they came from', async () => {
    await render(SignInPage, { inputs: { returnUrl: '/orders/42' }, routes: [{ path: 'orders/:id', children: [] }] });
    const user = userEvent.setup();

    await user.type(screen.getByLabelText('Email'), 'ada@example.com');
    await user.type(screen.getByLabelText('Password'), 'correct-horse');
    await user.click(screen.getByRole('button', { name: 'Sign in' }));

    await vi.waitFor(() => expect(TestBed.inject(Router).url).toBe('/orders/42'));
    expect(TestBed.inject(Session).user()).toEqual({ email: 'ada@example.com', token: 'test-token' });
  });

  it('says so when the password is wrong', async () => {
    await render(SignInPage);
    const user = userEvent.setup();

    await user.type(screen.getByLabelText('Email'), 'ada@example.com');
    await user.type(screen.getByLabelText('Password'), 'wrong');
    await user.click(screen.getByRole('button', { name: 'Sign in' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('Email or password is wrong.');
    expect(TestBed.inject(Session).signedIn()).toBe(false);
  });
});
