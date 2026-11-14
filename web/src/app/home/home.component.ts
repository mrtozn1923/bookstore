import { Component, OnInit } from '@angular/core';
import { NgOptimizedImage } from '@angular/common';
import { NewsletterComponent } from '../newsletter/newsletter.component';
import { SearchBoxComponent } from '../search-box/search-box.component';

@Component({
    selector: 'app-home',
    templateUrl: './home.component.html',
    styleUrls: ['./home.component.css'],
    imports: [NgOptimizedImage, NewsletterComponent, SearchBoxComponent]
})
export class HomeComponent implements OnInit {

  constructor() { }

  ngOnInit(): void {
  }

}
