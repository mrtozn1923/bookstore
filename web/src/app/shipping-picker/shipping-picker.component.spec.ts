import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ShippingPickerComponent } from './shipping-picker.component';

describe('ShippingPickerComponent', () => {
  let component: ShippingPickerComponent;
  let fixture: ComponentFixture<ShippingPickerComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ShippingPickerComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ShippingPickerComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
