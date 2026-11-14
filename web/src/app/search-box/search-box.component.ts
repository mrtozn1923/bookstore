import { AfterViewInit, Component, ElementRef, OnInit, ViewChild } from '@angular/core';

@Component({
    selector: 'app-search-box',
    template: `<input #searchInput placeholder="Search books...">`,
    standalone: true
})
export class SearchBoxComponent implements OnInit, AfterViewInit {
  @ViewChild('searchInput') searchInput!: ElementRef<HTMLInputElement>;

  ngOnInit(): void {
    console.log(this.searchInput); // undefined
  }

  ngAfterViewInit(): void {
    this.searchInput.nativeElement.focus();
  }
}
