import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestbedHarnessEnvironment } from '@angular/cdk/testing/testbed';
import { TestBed } from '@angular/core/testing';
import { MatCheckboxHarness } from '@angular/material/checkbox/testing';
import { MatPaginatorHarness } from '@angular/material/paginator/testing';
import { MatSelectHarness } from '@angular/material/select/testing';
import { MatSortHarness } from '@angular/material/sort/testing';
import { provideRouter, Router } from '@angular/router';
import { OrderRow } from '../../core/api-models';
import { OrdersPage } from './orders-page';

function row(id: number): OrderRow {
  return { id, placedAtUtc: '2026-11-01T10:00:00Z', customerName: 'Ada Lovelace', status: 'Shipped', itemCount: 1, total: 489, currency: 'TRY' };
}

async function render(inputs: Record<string, unknown> = {}) {
  TestBed.configureTestingModule({
    imports: [OrdersPage],
    providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
  });
  const fixture = TestBed.createComponent(OrdersPage);
  for (const [name, value] of Object.entries(inputs)) {
    fixture.componentRef.setInput(name, value);
  }
  const http = TestBed.inject(HttpTestingController);
  const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
  fixture.detectChanges();
  return { fixture, http, navigate, loader: TestbedHarnessEnvironment.loader(fixture) };
}

describe('OrdersPage', () => {
  it('turns the URL state into the API query, also when query params are missing', async () => {
    // withComponentInputBinding sets a missing query param to undefined
    const { http } = await render({ status: undefined, sort: 'total', dir: 'asc', page: '3' });
    const request = http.expectOne(r => r.url === '/api/admin/orders');
    expect(request.request.params.toString()).toBe('status=&sort=total&direction=asc&page=3&pageSize=25');
  });

  it('shows the server total in the paginator and keeps a zero-based page index', async () => {
    const { fixture, http, loader } = await render({ page: '2' });
    http.expectOne(r => r.url === '/api/admin/orders').flush({ items: [row(26), row(27)], total: 10000, page: 2, pageSize: 25 });
    await fixture.whenStable();
    const paginator = await loader.getHarness(MatPaginatorHarness);
    expect(await paginator.getRangeLabel()).toBe('26 – 50 of 10000');
  });

  it('starts from the first page when the status filter changes', async () => {
    const { fixture, http, navigate, loader } = await render({ page: '300' });
    http.expectOne(r => r.url === '/api/admin/orders').flush({ items: [row(1)], total: 10000, page: 300, pageSize: 25 });
    await fixture.whenStable();
    const select = await loader.getHarness(MatSelectHarness);
    await select.open();
    await select.clickOptions({ text: 'Cancelled' });
    expect(navigate).toHaveBeenLastCalledWith([], { queryParams: { status: 'Cancelled', page: null }, queryParamsHandling: 'merge' });
  });

  it('writes the sort into the URL and resets the page', async () => {
    const { fixture, http, navigate, loader } = await render();
    http.expectOne(r => r.url === '/api/admin/orders').flush({ items: [row(1)], total: 1, page: 1, pageSize: 25 });
    await fixture.whenStable();
    const total = (await (await loader.getHarness(MatSortHarness)).getSortHeaders({ label: 'Total' }))[0];
    await total.click();
    expect(navigate).toHaveBeenLastCalledWith([], { queryParams: { sort: 'total', dir: 'asc', page: null }, queryParamsHandling: 'merge' });
  });

  it('selects and deselects every order on the page from the header checkbox', async () => {
    const { fixture, http, loader } = await render();
    http.expectOne(r => r.url === '/api/admin/orders').flush({ items: [row(1), row(2), row(3)], total: 3, page: 1, pageSize: 25 });
    await fixture.whenStable();
    // The header checkbox has only an aria-label, no visible label text
    const [all] = await loader.getAllHarnesses(MatCheckboxHarness);
    expect(await all.getAriaLabel()).toBe('Select all orders on this page');
    const selected = () => (fixture.nativeElement as HTMLElement).querySelector('[data-testid="selected"]')?.textContent;

    await all.check();
    expect(selected()).toBe('3 selected');
    await all.uncheck();
    expect(selected()).toBe('0 selected');
  });
});
