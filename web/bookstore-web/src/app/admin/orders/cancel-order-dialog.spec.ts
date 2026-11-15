import { TestbedHarnessEnvironment } from '@angular/cdk/testing/testbed';
import { Component, inject } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { MatButtonHarness } from '@angular/material/button/testing';
import { MATERIAL_ANIMATIONS } from '@angular/material/core';
import { MatDialog } from '@angular/material/dialog';
import { MatDialogHarness } from '@angular/material/dialog/testing';
import { MatSelectHarness } from '@angular/material/select/testing';
import { CancelOrderData, CancelOrderDialog, CancelOrderResult } from './cancel-order-dialog';

@Component({ template: '' })
class Host {
  readonly dialog = inject(MatDialog);
}

async function open(data: CancelOrderData) {
  TestBed.configureTestingModule({
    imports: [Host],
    // No open/close animations: afterClosed() emits right after close()
    providers: [{ provide: MATERIAL_ANIMATIONS, useValue: { animationsDisabled: true } }],
  });
  const fixture = TestBed.createComponent(Host);
  const ref = fixture.componentInstance.dialog.open<CancelOrderDialog, CancelOrderData, CancelOrderResult>(CancelOrderDialog, { data });
  const results: (CancelOrderResult | undefined)[] = [];
  let completed = false;
  ref.afterClosed().subscribe({ next: result => results.push(result), complete: () => (completed = true) });
  // Dialogs live in the overlay, outside the fixture: use the document root loader
  const loader = TestbedHarnessEnvironment.documentRootLoader(fixture);
  return { loader, results, isCompleted: () => completed };
}

describe('CancelOrderDialog', () => {
  it('asks for a reason before it can cancel and returns it', async () => {
    const { loader, results, isCompleted } = await open({ orderIds: [42], summary: 'Ada Lovelace · ₺489.00' });
    const dialog = await loader.getHarness(MatDialogHarness);
    expect(await dialog.getTitleText()).toBe('Cancel order #42?');

    const confirm = await loader.getHarness(MatButtonHarness.with({ text: 'Cancel order' }));
    expect(await confirm.isDisabled()).toBe(true);
    const reason = await loader.getHarness(MatSelectHarness);
    await reason.open();
    await reason.clickOptions({ text: 'Out of stock' });
    await confirm.click();

    expect(results).toEqual([{ reason: 'Out of stock' }]);
    expect(isCompleted()).toBe(true);
  });

  it('returns undefined when the user keeps the order', async () => {
    const { loader, results } = await open({ orderIds: [1, 2], summary: '2 selected orders' });
    expect(await (await loader.getHarness(MatDialogHarness)).getTitleText()).toBe('Cancel 2 orders?');
    await (await loader.getHarness(MatButtonHarness.with({ text: 'Keep order' }))).click();
    expect(results).toEqual([undefined]);
  });
});
