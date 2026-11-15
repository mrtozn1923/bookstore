import { inject, RESPONSE_INIT } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { CanActivateFn, RedirectCommand, Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { API_BASE } from '../api';
import { normalizeCatalogQuery } from './catalog-query';

// ?page=1 ya da farklı sıralı parametre: tek yazıma kalıcı redirect
export const catalogQueryGuard: CanActivateFn = async (route, state) => {
  // inject() yalnız ilk await'ten önce çalışır
  const http = inject(HttpClient);
  const api = inject(API_BASE);
  const router = inject(Router);
  const responseInit = inject(RESPONSE_INIT, { optional: true });

  const result = normalizeCatalogQuery(route.queryParams);
  if (!result.valid || !result.changed) return true; // geçersizse sayfa 404'ü kendisi verir

  // hedef sonuçsuzsa redirect etme: 404 bu URL'de verilsin (301 -> 404 zinciri olmasın)
  const path = [route.params['genre'], route.params['language']].filter(Boolean).join('/');
  try {
    await firstValueFrom(http.get(`${api}/api/catalog/${path}`, { params: result.params }));
  } catch {
    return true;
  }

  if (responseInit) responseInit.status = 301;
  return new RedirectCommand(router.createUrlTree([state.url.split('?')[0]], { queryParams: result.params }));
};
