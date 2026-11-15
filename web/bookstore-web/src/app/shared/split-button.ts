import { CdkMenu, CdkMenuItem, CdkMenuTrigger } from '@angular/cdk/menu';
import { Component, input, output } from '@angular/core';
import { MatButton } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';

export interface SplitAction {
  id: string;
  label: string;
}

// Material has no split button: a main action plus a menu of related actions.
// Behavior from the CDK (menu, keyboard, focus), look from Material buttons and system tokens.
@Component({
  selector: 'app-split-button',
  imports: [CdkMenuTrigger, CdkMenu, CdkMenuItem, MatButton, MatIcon],
  template: `
    <div class="split" role="group" [attr.aria-label]="label()">
      <button matButton="filled" type="button" class="main" [disabled]="disabled()" (click)="run.emit(primary().id)">
        {{ primary().label }}
      </button>
      <button matButton="filled" type="button" class="toggle" [disabled]="disabled()" [cdkMenuTriggerFor]="menu"
              [attr.aria-label]="'More ' + label() + ' actions'">
        <mat-icon>arrow_drop_down</mat-icon>
      </button>
    </div>
    <ng-template #menu>
      <div class="split-menu" cdkMenu>
        @for (action of more(); track action.id) {
          <button type="button" class="split-menu-item" cdkMenuItem (cdkMenuItemTriggered)="run.emit(action.id)">
            {{ action.label }}
          </button>
        }
      </div>
    </ng-template>
  `,
  styleUrl: './split-button.scss',
})
export class SplitButton {
  readonly label = input.required<string>();
  readonly primary = input.required<SplitAction>();
  readonly more = input<SplitAction[]>([]);
  readonly disabled = input(false);
  readonly run = output<string>();
}
