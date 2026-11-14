import { Injectable } from '@angular/core';
import { CanDeactivate } from '@angular/router';
import { AddressEditComponent } from './address-edit/address-edit.component';

@Injectable({ providedIn: 'root' })
export class UnsavedChangesGuard implements CanDeactivate<AddressEditComponent> {

  canDeactivate(component: AddressEditComponent): boolean {
    if (!component.hasUnsavedChanges) {
      return true;
    }
    return confirm('You have unsaved changes. Are you sure you want to leave this page?');
  }
}
