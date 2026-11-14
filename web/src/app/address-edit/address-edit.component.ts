import { Component, ViewChild, ViewContainerRef, ChangeDetectionStrategy } from '@angular/core';
import { NoticeComponent } from '../notice/notice.component';
import { ButtonComponent } from '../button/button.component';

@Component({
    selector: 'app-address-edit',
    templateUrl: './address-edit.component.html',
    styleUrls: ['./address-edit.component.css'],
    changeDetection: ChangeDetectionStrategy.Eager,
    imports: [ButtonComponent]
})
export class AddressEditComponent {
  @ViewChild('noticeHost', { read: ViewContainerRef }) noticeHost!: ViewContainerRef;
  hasUnsavedChanges = false;

  save(): void {
    this.hasUnsavedChanges = false;
    this.noticeHost.clear();
    const ref = this.noticeHost.createComponent(NoticeComponent);
    ref.instance.message = 'Shipping address saved';
  }
}
