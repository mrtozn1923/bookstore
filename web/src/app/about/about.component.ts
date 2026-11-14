import { Component, OnInit } from '@angular/core';
import { CardComponent } from '../card/card.component';
import { BookCardComponent } from '../book-card/book-card.component';

@Component({
    selector: 'app-about',
    templateUrl: './about.component.html',
    styleUrls: ['./about.component.css'],
    imports: [CardComponent, BookCardComponent]
})
export class AboutComponent implements OnInit {

  constructor() { }

  ngOnInit(): void {
  }

}
