import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { IMAGE_LOADER, ImageLoaderConfig } from '@angular/common';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { provideClientHydration } from '@angular/platform-browser';
import { provideHttpClient, withFetch } from '@angular/common/http';
import { routes } from './app.routes';

// kapaklar CDN'de {stem}-{width}.jpg adıyla duruyor: 320, 640 ve 1200 px
const COVER_WIDTHS = [320, 640, 1200];

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes, withComponentInputBinding()),
    provideClientHydration(),
    provideHttpClient(withFetch()),
    {
      provide: IMAGE_LOADER,
      useValue: (config: ImageLoaderConfig) => {
        // istenen genişliğe en yakın, ondan büyük ya da eşit dosya
        const width = COVER_WIDTHS.find((w) => w >= (config.width ?? 640)) ?? 1200;
        return `https://cdn.bookstore.example/images/covers/${config.src}-${width}.jpg`;
      },
    },
  ],
};
