import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { appVersion } from './version';
import { VersionUpdate } from './version-update';

@Component({
  imports: [RouterOutlet],
  selector: 'app-root',
  styleUrl: './app.css',
  templateUrl: './app.html',
})
export class App {
  protected readonly version = appVersion;
  protected readonly update = inject(VersionUpdate);
}
