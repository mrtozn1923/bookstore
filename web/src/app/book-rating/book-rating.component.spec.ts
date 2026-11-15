import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Component, signal } from '@angular/core';
import { FormField, form, required } from '@angular/forms/signals';
import { BookRatingComponent } from './book-rating.component';

@Component({
  imports: [BookRatingComponent, FormField],
  template: `<app-book-rating [formField]="reviewForm.stars" />`
})
class ReviewHost {
  readonly review = signal({ stars: 4 });
  readonly reviewForm = form(this.review, (r) => required(r.stars));
}

describe('BookRatingComponent', () => {
  let component: BookRatingComponent;
  let fixture: ComponentFixture<BookRatingComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ BookRatingComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(BookRatingComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('works as a FormValueControl with [formField]', async () => {
    const host = TestBed.createComponent(ReviewHost);
    host.autoDetectChanges();
    await host.whenStable();
    const stars = host.nativeElement.querySelectorAll('button');
    expect(stars.length).toBe(5);
    expect(host.nativeElement.querySelectorAll('button.filled').length).toBe(4);
    stars[1].click();
    stars[1].dispatchEvent(new Event('blur'));
    expect(host.componentInstance.review()).toEqual({ stars: 2 });
    expect(host.componentInstance.reviewForm.stars().touched()).toBe(true);
  });
});
