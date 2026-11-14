import { Component } from '@angular/core';
import { CatalogModule } from './catalog/catalog.module';

@Component({
  imports: [CatalogModule],
  selector: 'app-root',
  templateUrl: './app.html',
})
export class App {}
