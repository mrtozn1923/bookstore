import { Component, ChangeDetectionStrategy } from '@angular/core';
import { TrackClickDirective } from '../track-click.directive';

@Component({
  selector: 'app-button',
  template: `<button type="button"><ng-content></ng-content></button>`,
  changeDetection: ChangeDetectionStrategy.Eager,
  hostDirectives: [
    { directive: TrackClickDirective, inputs: ['trackId'] }
  ]
})
export class ButtonComponent { }
