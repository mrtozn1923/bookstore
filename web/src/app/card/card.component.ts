import { AfterContentInit, Component, ContentChild, ElementRef } from '@angular/core';

@Component({
    selector: 'app-card',
    templateUrl: './card.component.html',
    styleUrls: ['./card.component.css'],
    standalone: true
})
export class CardComponent implements AfterContentInit {
  @ContentChild('cardTitle') cardTitle?: ElementRef;

  ngAfterContentInit(): void {
    console.log('Title projected?', !!this.cardTitle);
  }
}
