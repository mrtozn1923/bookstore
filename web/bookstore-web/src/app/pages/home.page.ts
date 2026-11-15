import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-home-page',
  imports: [RouterLink],
  template: `
    <h1>Bookstore Blog</h1>
    <a routerLink="/blog/search-console-api">Search Console API in C#</a>
  `,
})
export class HomePage {}
