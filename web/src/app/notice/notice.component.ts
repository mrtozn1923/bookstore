import { Component } from '@angular/core';

@Component({
  selector: 'app-notice',
  template: `<div class="notice">{{ message }}</div>`
})
export class NoticeComponent {
  message = '';
}
