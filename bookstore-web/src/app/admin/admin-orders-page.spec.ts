import { TestBed } from '@angular/core/testing';
import { MockStore, provideMockStore } from '@ngrx/store/testing';
import type { AdminOrder } from '../models';
import { AdminOrdersPage } from './admin-orders-page';
import { adminOrdersPageActions } from './orders.actions';
import { adminOrdersFeature } from './orders.feature';

describe('AdminOrdersPage', () => {
  it('shows the selected orders and dispatches Ship Clicked', async () => {
    TestBed.configureTestingModule({
      imports: [AdminOrdersPage],
      // MockStore: selectors return fixed values, dispatched actions only reach the spy
      providers: [
        provideMockStore({
          selectors: [
            { selector: adminOrdersFeature.selectVisibleOrders, value: [{ id: 1042, customer: 'Grace', total: 430, status: 'new' } as AdminOrder] },
            { selector: adminOrdersFeature.selectNewCount, value: 1 },
            { selector: adminOrdersFeature.selectStatusFilter, value: 'new' },
            { selector: adminOrdersFeature.selectShippingIds, value: [] },
            { selector: adminOrdersFeature.selectError, value: null },
          ],
        }),
      ],
    });
    const store = TestBed.inject(MockStore);
    const dispatch = vi.spyOn(store, 'dispatch');
    const fixture = TestBed.createComponent(AdminOrdersPage);
    await fixture.whenStable();

    const element: HTMLElement = fixture.nativeElement;
    expect(element.querySelector('h1')?.textContent).toContain('(1 new)');
    element.querySelector<HTMLButtonElement>('button.ship')!.click();
    expect(dispatch).toHaveBeenCalledWith(adminOrdersPageActions.shipClicked({ id: 1042 }));
  });
});
