import { Component } from '@angular/core';

@Component({
  selector: 'app-address-edit',
  templateUrl: './address-edit.component.html',
  styleUrls: ['./address-edit.component.css']
})
export class AddressEditComponent {
  hasUnsavedChanges = false;

  save(): void {
    this.hasUnsavedChanges = false;
  }
}
