import { HttpClient, HttpEventType } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';

interface CoverResult {
  isbn: string;
  bytes: number;
}

@Component({
  selector: 'app-cover-upload-page',
  template: `
    <h1>Upload a cover for Dune</h1>
    <input type="file" accept="image/*" (change)="upload($any($event.target).files[0])" />
    <p>{{ progress() }}</p>
  `,
})
export class CoverUploadPage {
  private readonly http = inject(HttpClient);
  protected readonly progress = signal('');

  upload(file: File | undefined) {
    if (!file) return;
    this.http
      .post<CoverResult>('/api/covers/9780441172719', file, {
        observe: 'events',
        reportUploadProgress: true,
      })
      .subscribe((event) => {
        if (event.type === HttpEventType.UploadProgress) {
          const percent = Math.round((100 * event.loaded) / (event.total ?? file.size));
          this.progress.set(`${percent}%`);
          console.log(`upload ${event.loaded} of ${event.total}`);
        } else if (event.type === HttpEventType.Response) {
          this.progress.set(`done: ${event.body?.bytes} bytes`);
          console.log(`server received ${event.body?.bytes} bytes`);
        }
      });
  }
}
