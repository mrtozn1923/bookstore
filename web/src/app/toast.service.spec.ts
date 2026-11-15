import { TestBed } from '@angular/core/testing';
import { ToastService } from './toast.service';

describe('ToastService', () => {
  it('keeps toasts until dismissed', () => {
    const service = TestBed.inject(ToastService);
    service.show('Dune added to the cart', 60000);
    expect(service.items().map((t) => t.text)).toEqual(['Dune added to the cart']);
    service.dismiss(service.items()[0].id);
    expect(service.items()).toEqual([]);
  });
});
