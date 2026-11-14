import { httpResource } from '@angular/common/http';
import { Component, ElementRef, inject, input, OnDestroy, viewChild } from '@angular/core';
import { Book } from '../catalog/book';
import { DeviceCapabilities } from '../core/device-capabilities';

@Component({
  selector: 'app-preview-page',
  template: `
    <section #reader class="reader">
      @if (book.hasValue()) {
        <h1>{{ book.value().title }}: sample</h1>
        <div class="tools">
          @if (device.supports.wakeLock) {
            <button class="screen" (click)="device.keepScreenOn(!device.screenOn())">
              {{ device.screenOn() ? 'Let the screen sleep' : 'Keep the screen on' }}
            </button>
          }
          @if (device.supports.fullscreen) {
            <button class="fullscreen" (click)="toggleFullscreen()">
              {{ device.fullscreen() ? 'Exit full screen' : 'Full screen' }}
            </button>
          }
        </div>
        @for (page of pages; track page) {
          <p class="page">
            Sample page {{ page }} of {{ book.value().title }}. (Placeholder text.)
          </p>
        }
      }
    </section>
  `,
  styles: `
    .reader:fullscreen {
      background: #fffdf7;
      padding: 2rem;
      overflow: auto;
    }
    .reader:fullscreen .page {
      font-size: 1.25rem;
      max-width: 40rem;
    }
  `,
})
export class PreviewPage implements OnDestroy {
  readonly isbn = input.required<string>();
  protected readonly book = httpResource<Book>(() => `/api/books/${this.isbn()}`);
  protected readonly device = inject(DeviceCapabilities);
  protected readonly pages = [1, 2, 3];
  private readonly reader = viewChild.required<ElementRef<HTMLElement>>('reader');

  async toggleFullscreen() {
    try {
      // only the reader goes full screen, not the whole app with its header
      await this.device.toggleFullscreen(this.reader().nativeElement);
    } catch (err) {
      console.log(`fullscreen: ${(err as Error).name}`);
    }
  }

  ngOnDestroy() {
    // leaving the preview: give the screen back and leave full screen
    void this.device.keepScreenOn(false);
    if (document.fullscreenElement) void document.exitFullscreen();
  }
}
