import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { StockRow } from '../../core/api-models';
import { StockPage } from './stock-page';

const rows: StockRow[] = [
  { id: 1, isbn: '9786050000001', title: 'The Hobbit · Hardcover #1', warehouse: 'Istanbul', onHand: 63, reserved: 36 },
  { id: 2, isbn: '9786050000002', title: 'Emma · Hardcover #2', warehouse: 'Izmir', onHand: 397, reserved: 3 },
  { id: 3, isbn: '9786050000003', title: 'Middlemarch · Hardcover #3', warehouse: 'Izmir', onHand: 12, reserved: 0 },
];

async function render() {
  TestBed.configureTestingModule({ imports: [StockPage], providers: [provideHttpClient(), provideHttpClientTesting()] });
  const fixture = TestBed.createComponent(StockPage);
  const http = TestBed.inject(HttpTestingController);
  TestBed.tick();
  const element = fixture.nativeElement as HTMLElement;
  const count = () => element.querySelector('[data-testid="row-count"]')?.textContent;
  return { fixture, http, element, count };
}

describe('StockPage', () => {
  it('searches the loaded rows by title or ISBN, because Ctrl+F cannot see rows that are not rendered', async () => {
    const { fixture, http, element, count } = await render();
    http.expectOne(r => r.url === '/api/admin/stock').flush(rows);
    await fixture.whenStable();
    expect(count()).toBe('3 rows');
    const input = element.querySelector<HTMLInputElement>('input[type="search"]')!;
    input.value = 'middlemarch';
    input.dispatchEvent(new Event('input'));
    await fixture.whenStable();
    expect(count()).toBe('1 rows');
    input.value = '0000002';
    input.dispatchEvent(new Event('input'));
    await fixture.whenStable();
    expect(count()).toBe('1 rows');
  });

  it('loads one warehouse when it is picked in the listbox', async () => {
    const { fixture, http, element } = await render();
    http.expectOne(r => r.url === '/api/admin/stock').flush(rows);
    await fixture.whenStable();
    const izmir = [...element.querySelectorAll<HTMLElement>('[role="option"]')].find(o => o.textContent?.trim() === 'Izmir')!;
    izmir.click();
    TestBed.tick();
    const request = http.expectOne(r => r.url === '/api/admin/stock');
    expect(request.request.params.get('warehouse')).toBe('Izmir');
    request.flush(rows.filter(r => r.warehouse === 'Izmir'));
    await fixture.whenStable();
    expect(izmir.getAttribute('aria-selected')).toBe('true');
  });
});
