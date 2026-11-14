import { HttpClient, HttpEvent } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, switchMap, takeWhile, timer } from 'rxjs';

export interface ImportStatus {
  id: string; fileName: string; status: string; totalRows: number; invalidRows: number;
  committedRows: number; failureReason: string | null; duplicate: boolean;
}

const pending = ['Queued', 'Loading', 'Validating'];

@Injectable({ providedIn: 'root' })
export class UserImportApi {
  private readonly http = inject(HttpClient);

  // HttpEventType.UploadProgress events arrive for the upload progress
  upload(file: File): Observable<HttpEvent<ImportStatus>> {
    const form = new FormData();
    form.append('file', file, file.name);
    return this.http.post<ImportStatus>('/api/user-imports', form, { reportProgress: true, observe: 'events' });
  }

  // ask every 2 seconds until the work is done; also emit the final (finished) status
  watch(id: string): Observable<ImportStatus> {
    return timer(0, 2000).pipe(
      switchMap(() => this.http.get<ImportStatus>(`/api/user-imports/${id}`)),
      takeWhile(s => pending.includes(s.status), true)
    );
  }

  commit(id: string, mode: 'AllOrNothing' | 'ValidOnly'): Observable<ImportStatus> {
    return this.http.post<ImportStatus>(`/api/user-imports/${id}/commit`, null, { params: { mode } });
  }
}
