import { ComponentFixture, TestBed } from '@angular/core/testing';
import { WishlistButtonComponent } from './wishlist-button.component';

describe('WishlistButtonComponent', () => {
  let component: WishlistButtonComponent;
  let fixture: ComponentFixture<WishlistButtonComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [WishlistButtonComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(WishlistButtonComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should increase the counter when the button is clicked', () => {
    const element = fixture.nativeElement as HTMLElement;
    vi.spyOn(component, 'add');

    element.querySelector('button')!.click();
    fixture.detectChanges();

    expect(component.add).toHaveBeenCalledTimes(1);
    expect(element.querySelector('.count')?.textContent).toBe('1');
  });
});
