import { Component } from '@angular/core';
import { TrackClickDirective } from '../track-click.directive';

@Component({
  selector: 'app-button',
  standalone: true,
  template: `<button type="button"><ng-content></ng-content></button>`,
  hostDirectives: [
    { directive: TrackClickDirective, inputs: ['trackId'] }
  ]
})
export class ButtonComponent { }
