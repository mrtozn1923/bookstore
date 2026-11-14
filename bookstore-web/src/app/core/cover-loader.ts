import { ImageLoaderConfig } from '@angular/common';

// The widths Bookstore.Api produces; NgOptimizedImage builds the srcset from the same list
export const COVER_WIDTHS = [160, 320, 480, 640, 960, 1200];

// '/covers/dune.jpg' at 480px -> '/img/covers/dune?w=480' (resized and encoded by Bookstore.Api)
export function coverLoader(config: ImageLoaderConfig): string {
  const name = config.src.replace(/^\/covers\//, '').replace(/\.jpg$/, '');
  return `/img/covers/${name}?w=${config.width ?? 1200}`;
}
