import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { CdkConnectedOverlay, CdkOverlayOrigin, ConnectedPosition } from '@angular/cdk/overlay';
import { afterNextRender, Component, computed, ElementRef, inject, Injector, signal, viewChild } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { MatButton, MatIconButton } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';
import { MatListItem, MatListItemIcon, MatListItemTitle, MatNavList } from '@angular/material/list';
import { MatSidenav, MatSidenavContainer, MatSidenavContent } from '@angular/material/sidenav';
import { MatToolbar } from '@angular/material/toolbar';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { map } from 'rxjs';
import { tabLeavesPanel } from '../core/overlay-focus';

@Component({
  selector: 'app-admin-shell',
  imports: [
    MatToolbar, MatSidenavContainer, MatSidenav, MatSidenavContent, MatNavList, MatListItem, MatListItemIcon,
    MatListItemTitle, MatIcon, MatButton, MatIconButton, RouterOutlet, RouterLink, RouterLinkActive, CdkOverlayOrigin,
    CdkConnectedOverlay,
  ],
  templateUrl: './admin-shell.html',
  styleUrl: './admin-shell.scss',
})
export class AdminShell {
  // CDK Breakpoints.Handset: narrower than 600px in portrait or 960px in landscape
  protected readonly handset = toSignal(
    inject(BreakpointObserver).observe(Breakpoints.Handset).pipe(map(state => state.matches)),
    { initialValue: false },
  );
  protected readonly mode = computed(() => (this.handset() ? 'over' : 'side'));

  private readonly injector = inject(Injector);
  private readonly accountPanel = viewChild<ElementRef<HTMLElement>>('accountPanel');

  // Demo user until the Identity module has a sign-in
  protected readonly user = { name: 'Ada Lovelace', email: 'ada.lovelace@example.com', role: 'StoreStaff' };
  protected readonly accountOpen = signal(false);
  // Below the button, right edges aligned; above it if there is no room below
  protected readonly accountPositions: ConnectedPosition[] = [
    { originX: 'end', originY: 'bottom', overlayX: 'end', overlayY: 'top', offsetY: 4 },
    { originX: 'end', originY: 'top', overlayX: 'end', overlayY: 'bottom', offsetY: -4 },
  ];

  protected focusFirst(): void {
    // The panel's DOM exists only after the next render
    afterNextRender(() => this.accountPanel()?.nativeElement.querySelector<HTMLElement>('a, button')?.focus(),
      { injector: this.injector });
  }

  // Escape is handled by the directive itself (it detaches); Tab out of the panel closes it as well
  protected accountKeydown(event: KeyboardEvent): void {
    const panel = this.accountPanel()?.nativeElement;
    if (panel && tabLeavesPanel(event, panel)) {
      event.preventDefault();
      this.accountOpen.set(false);
    }
  }

  // detach: the directive closed the panel (Escape); outside click: we close it, focus stays where the user clicked
  protected closeAccount(button: HTMLElement, returnFocus: boolean): void {
    const focusInside = this.accountPanel()?.nativeElement.contains(document.activeElement) ?? false;
    this.accountOpen.set(false);
    if (returnFocus && (focusInside || document.activeElement === document.body)) {
      button.focus();
    }
  }

  protected readonly links = [
    { path: 'dashboard', icon: 'dashboard', label: 'Dashboard' },
    { path: 'orders', icon: 'receipt_long', label: 'Orders' },
    { path: 'institutional-orders/new', icon: 'assignment', label: 'Institutional order' },
    { path: 'institutional-orders/1042/shipments', icon: 'local_shipping', label: 'Shipments' },
  ];
}
