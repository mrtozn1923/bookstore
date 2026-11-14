import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { HomeComponent } from './home/home.component';
import { BookListComponent } from './catalog/book-list/book-list.component';
import { BookDetailComponent } from './book-detail/book-detail.component';
import { BookSummaryComponent } from './book-summary/book-summary.component';
import { BookReviewsComponent } from './book-reviews/book-reviews.component';
import { AboutComponent } from './about/about.component';
import { NotFoundComponent } from './not-found/not-found.component';

const routes: Routes = [
  { path: '', redirectTo: 'home', pathMatch: 'full' },
  { path: 'home', component: HomeComponent },
  { path: 'books', component: BookListComponent },
  {
    path: 'books/:isbn',
    component: BookDetailComponent,
    children: [
      { path: '', redirectTo: 'summary', pathMatch: 'full' },
      { path: 'summary', component: BookSummaryComponent },
      { path: 'reviews', component: BookReviewsComponent }
    ]
  },
  { path: 'about', component: AboutComponent },
  { path: '**', component: NotFoundComponent }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
