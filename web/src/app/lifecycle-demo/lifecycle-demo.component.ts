import {
  AfterContentChecked, AfterContentInit, AfterViewChecked, AfterViewInit,
  Component, DoCheck, Input, OnChanges, OnDestroy, OnInit, SimpleChanges
} from '@angular/core';

@Component({
    selector: 'app-lifecycle-demo',
    template: `<p>Count: {{ count }}</p>`,
})
export class LifecycleDemoComponent implements OnChanges, OnInit, DoCheck,
  AfterContentInit, AfterContentChecked, AfterViewInit, AfterViewChecked, OnDestroy {
  @Input() count = 0;

  constructor() { this.log('constructor'); }
  ngOnChanges(changes: SimpleChanges): void { this.log('ngOnChanges'); }
  ngOnInit(): void { this.log('ngOnInit'); }
  ngDoCheck(): void { this.log('ngDoCheck'); }
  ngAfterContentInit(): void { this.log('ngAfterContentInit'); }
  ngAfterContentChecked(): void { this.log('ngAfterContentChecked'); }
  ngAfterViewInit(): void { this.log('ngAfterViewInit'); }
  ngAfterViewChecked(): void { this.log('ngAfterViewChecked'); }
  ngOnDestroy(): void { this.log('ngOnDestroy'); }

  private log(hook: string): void {
    console.log(`[demo] ${hook}`);
  }
}
