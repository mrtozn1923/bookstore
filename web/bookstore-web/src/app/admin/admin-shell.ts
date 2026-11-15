import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { Component, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { MatButton, MatIconButton } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';
import { MatListItem, MatListItemIcon, MatListItemTitle, MatNavList } from '@angular/material/list';
import { MatSidenav, MatSidenavContainer, MatSidenavContent } from '@angular/material/sidenav';
import { MatToolbar } from '@angular/material/toolbar';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { map } from 'rxjs';

@Component({
  selector: 'app-admin-shell',
  imports: [
    MatToolbar, MatSidenavContainer, MatSidenav, MatSidenavContent, MatNavList, MatListItem, MatListItemIcon,
    MatListItemTitle, MatIcon, MatButton, MatIconButton, RouterOutlet, RouterLink, RouterLinkActive,
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

  protected readonly links = [
    { path: 'dashboard', icon: 'dashboard', label: 'Dashboard' },
    { path: 'institutional-orders/new', icon: 'assignment', label: 'Institutional order' },
  ];
}
