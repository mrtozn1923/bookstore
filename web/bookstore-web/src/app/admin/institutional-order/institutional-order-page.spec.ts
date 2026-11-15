import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestbedHarnessEnvironment } from '@angular/cdk/testing/testbed';
import { TestBed } from '@angular/core/testing';
import { MatButtonHarness } from '@angular/material/button/testing';
import { MatDatepickerInputHarness } from '@angular/material/datepicker/testing';
import { MatFormFieldHarness } from '@angular/material/form-field/testing';
import { MatSelectHarness } from '@angular/material/select/testing';
import { InstitutionUnit } from '../../core/api-models';
import { InstitutionalOrderPage } from './institutional-order-page';

const units: InstitutionUnit[] = [
  { id: 1, name: 'Main Campus Library', kind: 'Campus' },
  { id: 2, name: 'North Campus Library', kind: 'Campus' },
  { id: 3, name: 'Science Department', kind: 'Department' },
];

async function render() {
  TestBed.configureTestingModule({
    imports: [InstitutionalOrderPage],
    providers: [provideHttpClient(), provideHttpClientTesting()],
  });
  const fixture = TestBed.createComponent(InstitutionalOrderPage);
  fixture.detectChanges();
  TestBed.inject(HttpTestingController).expectOne('/api/institutions/1/units').flush(units);
  await fixture.whenStable();
  return { fixture, loader: TestbedHarnessEnvironment.loader(fixture) };
}

describe('InstitutionalOrderPage', () => {
  beforeEach(() => localStorage.clear());

  it('selects the last used units again although the options are new objects', async () => {
    localStorage.setItem('bookstore.admin.lastUnits', JSON.stringify([units[0], units[2]]));
    const { loader } = await render();
    const select = await loader.getHarness(MatSelectHarness);
    expect(await select.getValueText()).toBe('Main Campus Library, Science Department');
  });

  it('shows the errors of untouched fields only after submit', async () => {
    const { loader } = await render();
    const unitsField = await loader.getHarness(MatFormFieldHarness.with({ floatingLabelText: 'Deliver to units' }));
    const emailField = await loader.getHarness(MatFormFieldHarness.with({ floatingLabelText: 'Contact e-mail' }));
    expect(await unitsField.getTextErrors()).toEqual([]);
    expect(await emailField.getTextErrors()).toEqual([]);

    await (await loader.getHarness(MatButtonHarness.with({ text: 'Create order' }))).click();
    expect(await unitsField.getTextErrors()).toEqual(['Choose at least one unit.']);
    // The e-mail field uses its own matcher; an empty required field must still show its error
    expect(await emailField.getTextErrors()).toEqual(['Enter a contact e-mail.']);
  });

  it('reads Turkish dates as day.month.year', async () => {
    const { fixture, loader } = await render();
    const year = new Date().getFullYear() + 1;
    const date = await loader.getHarness(MatDatepickerInputHarness);
    await date.setValue(`05.12.${year}`);
    await date.blur();

    const picked = fixture.componentInstance['model']().deliveryDate as Date;
    expect([picked.getDate(), picked.getMonth() + 1, picked.getFullYear()]).toEqual([5, 12, year]);
    expect(await date.getValue()).toBe(`05.12.${year}`);
  });

  it('sends the picked day as a date, not as UTC midnight', async () => {
    const { fixture, loader } = await render();
    const year = new Date().getFullYear() + 1;
    fixture.componentInstance['model'].set({
      units: [units[0]],
      book: { isbn: '9780261102217', title: 'The Hobbit', author: 'J.R.R. Tolkien', category: 'Fantasy', price: 329.9, publishedYear: 1937 },
      quantity: 40,
      deliveryDate: new Date(year, 11, 15),
      contactEmail: 'nevin@example.com',
      notes: '',
    });
    await (await loader.getHarness(MatButtonHarness.with({ text: 'Create order' }))).click();

    const request = JSON.parse(fixture.componentInstance['request']());
    expect(request.deliveryDate).toBe(`${year}-12-15`);
    expect(request.unitIds).toEqual([1]);
  });
});
