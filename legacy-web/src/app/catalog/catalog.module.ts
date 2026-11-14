import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { BookSearchComponent } from './book-search.component';
import { WishlistButtonComponent } from './wishlist-button.component';

@NgModule({
  declarations: [BookSearchComponent, WishlistButtonComponent],
  imports: [CommonModule, ReactiveFormsModule],
  exports: [BookSearchComponent, WishlistButtonComponent],
})
export class CatalogModule {}
