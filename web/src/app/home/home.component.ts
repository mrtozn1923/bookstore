import { Component, OnInit } from '@angular/core';
import { NgOptimizedImage } from '@angular/common';
import { NewsletterComponent } from '../newsletter/newsletter.component';

@Component({
    selector: 'app-home',
    templateUrl: './home.component.html',
    styleUrls: ['./home.component.css'],
    standalone: true,
    imports: [NgOptimizedImage, NewsletterComponent]
})
export class HomeComponent implements OnInit {

  constructor() { }

  ngOnInit(): void {
  }

}
