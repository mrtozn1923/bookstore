import { Directive, HostListener, Input } from '@angular/core';

@Directive({
  selector: '[appTrackClick]',
})
export class TrackClickDirective {
  @Input() trackId = '';

  @HostListener('click') onClick(): void {
    console.log(`click: ${this.trackId}`);
  }
}
