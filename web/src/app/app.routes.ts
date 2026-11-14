import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', loadComponent: () => import('./assistant/assistant').then((m) => m.Assistant) },
  { path: 'books/:isbn', loadComponent: () => import('./books/book-detail').then((m) => m.BookDetail) },
  { path: 'protocols/agui', loadComponent: () => import('./protocols/agui-chat').then((m) => m.AgUiChat) },
  { path: 'protocols/ai-sdk', loadComponent: () => import('./protocols/ai-sdk-chat').then((m) => m.AiSdkChat) },
];
