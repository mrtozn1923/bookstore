import { Component, ChangeDetectionStrategy } from '@angular/core';

@Component({
    selector: 'app-notice',
    changeDetection: ChangeDetectionStrategy.Eager,
    template: `<div class="notice">{{ message }}</div>`,
})
export class NoticeComponent {
  message = '';
}
