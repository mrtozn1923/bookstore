import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { BookListComponent } from './book-list/book-list.component';
import { BookItemComponent } from './book-item/book-item.component';
import { TruncatePipe } from './truncate.pipe';
import { InStockPipe } from './in-stock.pipe';



@NgModule({
  declarations: [
    BookListComponent,
    BookItemComponent,
    TruncatePipe,
    InStockPipe
  ],
  imports: [
    CommonModule
  ],
  exports: [
    BookListComponent
  ]
})
export class CatalogModule { }
