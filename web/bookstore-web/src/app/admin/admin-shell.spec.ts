import { BreakpointObserver, BreakpointState } from '@angular/cdk/layout';
import { TestbedHarnessEnvironment } from '@angular/cdk/testing/testbed';
import { TestBed } from '@angular/core/testing';
import { MatSidenavHarness } from '@angular/material/sidenav/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { AdminShell } from './admin-shell';

async function render(handset: boolean) {
  const state: BreakpointState = { matches: handset, breakpoints: {} };
  TestBed.configureTestingModule({
    imports: [AdminShell],
    providers: [provideRouter([]), { provide: BreakpointObserver, useValue: { observe: () => of(state) } }],
  });
  const fixture = TestBed.createComponent(AdminShell);
  const sidenav = await TestbedHarnessEnvironment.loader(fixture).getHarness(MatSidenavHarness);
  return { element: fixture.nativeElement as HTMLElement, sidenav };
}

describe('AdminShell', () => {
  it('keeps the navigation open beside the content on wide screens', async () => {
    const { element, sidenav } = await render(false);
    expect(await sidenav.getMode()).toBe('side');
    expect(await sidenav.isOpen()).toBe(true);
    expect(element.querySelector('button[aria-label="Open navigation"]')).toBeNull();
  });

  it('turns the navigation into a closed overlay with a menu button on handsets', async () => {
    const { element, sidenav } = await render(true);
    expect(await sidenav.getMode()).toBe('over');
    expect(await sidenav.isOpen()).toBe(false);
    expect(element.querySelector('button[aria-label="Open navigation"]')).not.toBeNull();
  });
});
