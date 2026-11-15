import { TestbedHarnessEnvironment } from '@angular/cdk/testing/testbed';
import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { SplitButton } from './split-button';
import { SplitButtonHarness } from './testing/split-button-harness';

@Component({
  imports: [SplitButton],
  template: `<app-split-button label="Save" [primary]="{ id: 'save', label: 'Save plan' }" [disabled]="disabled()"
                               [more]="[{ id: 'save-notify', label: 'Save and notify the unit' }]" (run)="runs.push($event)" />`,
})
class Host {
  readonly disabled = signal(false);
  readonly runs: string[] = [];
}

async function render() {
  const fixture = TestBed.createComponent(Host);
  const button = await TestbedHarnessEnvironment.loader(fixture).getHarness(SplitButtonHarness);
  return { fixture, button };
}

describe('SplitButton', () => {
  it('runs the main action and the actions in its menu', async () => {
    const { fixture, button } = await render();
    expect(await button.getPrimaryText()).toBe('Save plan');
    await button.clickPrimary();
    expect(await button.getMenuItems()).toEqual(['Save and notify the unit']);
    await button.chooseFromMenu('Save and notify the unit');
    expect(fixture.componentInstance.runs).toEqual(['save', 'save-notify']);
  });

  it('disables both parts together', async () => {
    const { fixture, button } = await render();
    fixture.componentInstance.disabled.set(true);
    expect(await button.isDisabled()).toBe(true);
  });
});
