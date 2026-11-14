import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';

import { AppComponent } from './app.component';
import { BookCardComponent } from './book-card/book-card.component';
import { CatalogModule } from './catalog/catalog.module';

@NgModule({
  declarations: [
    AppComponent,
    BookCardComponent
  ],
  imports: [
    BrowserModule,
    CatalogModule
  ],
  providers: [],
  bootstrap: [AppComponent]
})
export class AppModule { }
