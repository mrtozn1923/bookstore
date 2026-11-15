import { TestBed } from '@angular/core/testing';
import { ToastService } from '../toast.service';
import { ToastHostComponent } from './toast-host.component';

describe('ToastHostComponent', () => {
  it('shows a toast and removes it after its duration', async () => {
    const fixture = TestBed.createComponent(ToastHostComponent);
    fixture.autoDetectChanges();
    TestBed.inject(ToastService).show('Dune added to the cart', 50);
    await fixture.whenStable();
    expect(fixture.nativeElement.textContent.trim()).toBe('Dune added to the cart');
    await new Promise((resolve) => setTimeout(resolve, 80));
    await fixture.whenStable();
    expect(fixture.nativeElement.querySelector('.toast')).toBeNull();
  });
});
