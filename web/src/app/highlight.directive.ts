import { Directive, ElementRef, HostListener } from '@angular/core';

@Directive({
    selector: '[appHighlight]',
})
export class HighlightDirective {

  constructor(private el: ElementRef<HTMLElement>) { }

  @HostListener('mouseenter') onMouseEnter(): void {
    this.highlight('lightyellow');
  }

  @HostListener('mouseleave') onMouseLeave(): void {
    this.highlight('');
  }

  private highlight(color: string): void {
    this.el.nativeElement.style.backgroundColor = color;
  }
}
