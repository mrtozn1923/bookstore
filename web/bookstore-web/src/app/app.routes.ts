import { ActivatedRouteSnapshot, Routes } from '@angular/router';
import { HomePage } from './pages/home.page';
import { PostPage } from './pages/post.page';

export const routes: Routes = [
  { path: '', component: HomePage, title: 'Bookstore Blog' },
  {
    path: 'blog/:slug',
    component: PostPage,
    title: (route: ActivatedRouteSnapshot) => `${route.paramMap.get('slug')} - Bookstore Blog`,
  },
];
