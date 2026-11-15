import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestbedHarnessEnvironment } from '@angular/cdk/testing/testbed';
import { TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { InstitutionalOrder } from '../../core/api-models';
import { SplitButtonHarness } from '../../shared/testing/split-button-harness';
import { ShipmentPlanPage } from './shipment-plan-page';

const order: InstitutionalOrder = {
  id: 1042, unit: 'Main Campus Library',
  lines: [
    { id: 1, isbn: '9780441172719', title: 'Dune', quantity: 30, shipment: 'This' },
    { id: 2, isbn: '9780261102217', title: 'The Hobbit', quantity: 25, shipment: 'This' },
    { id: 4, isbn: '9780141439549', title: 'Middlemarch', quantity: 20, shipment: 'Next' },
  ],
};

async function render() {
  TestBed.configureTestingModule({ imports: [ShipmentPlanPage], providers: [provideHttpClient(), provideHttpClientTesting()] });
  const fixture = TestBed.createComponent(ShipmentPlanPage);
  const http = TestBed.inject(HttpTestingController);
  http.expectOne('/api/admin/institutional-orders/1042').flush(order);
  await fixture.whenStable();
  const element = fixture.nativeElement as HTMLElement;
  const column = (i: number) => [...element.querySelectorAll('.column')[i].querySelectorAll('.line .title')].map(t => t.textContent);
  const button = (name: string) => [...element.querySelectorAll('button')].find(b => b.textContent?.trim() === name || b.getAttribute('aria-label') === name)!;
  return { fixture, http, element, column, button };
}

describe('ShipmentPlanPage', () => {
  it('moves a line to the next shipment without dragging and saves the plan', async () => {
    const { fixture, http, column, button } = await render();
    expect(button('Save plan').disabled).toBe(true);
    button('Move The Hobbit to next shipment').click();
    await fixture.whenStable();
    expect(column(0)).toEqual(['Dune']);
    expect(column(1)).toEqual(['Middlemarch', 'The Hobbit']);
    button('Save plan').click();
    const request = http.expectOne('/api/admin/institutional-orders/1042/shipments');
    expect(request.request.body).toEqual({ thisShipment: [1], nextShipment: [4, 2] });
  });

  it('moves a line between the lists on drop and does not allow an empty first shipment', async () => {
    const { fixture, element, column, button } = await render();
    const lists = fixture.debugElement.queryAll(By.css('[cdkDropList]'));
    const from = { data: 'this' };
    const to = { data: 'next' };
    lists[1].triggerEventHandler('cdkDropListDropped', { previousContainer: from, container: to, previousIndex: 0, currentIndex: 0 });
    lists[1].triggerEventHandler('cdkDropListDropped', { previousContainer: from, container: to, previousIndex: 0, currentIndex: 2 });
    await fixture.whenStable();
    expect(column(0)).toEqual([]);
    expect(column(1)).toEqual(['Dune', 'Middlemarch', 'The Hobbit']);
    expect(button('Save plan').disabled).toBe(true);
    expect(element.querySelector('.hint')?.textContent).toContain('needs at least one line');
  });

  it('saves and notifies the unit from the split button menu', async () => {
    const { fixture, http, button } = await render();
    button('Move The Hobbit to next shipment').click();
    await fixture.whenStable();
    const save = await TestbedHarnessEnvironment.loader(fixture).getHarness(SplitButtonHarness);
    await save.chooseFromMenu('Save and notify the unit');
    const request = http.expectOne(r => r.url === '/api/admin/institutional-orders/1042/shipments');
    expect(request.request.params.get('notifyUnit')).toBe('true');
  });
});
