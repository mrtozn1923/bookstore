import { HttpClient, HttpErrorResponse, HttpResponse } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { Observable, map } from 'rxjs';
import { threeWayMerge } from './merge.js';

export interface InstitutionUserChanges { fullName: string; phone: string; spendingLimit: number | null; institutionUnitId: number; }
export interface InstitutionUser extends InstitutionUserChanges { id: number; }
export interface Versioned<T> { value: T; etag: string; }
export interface ConflictProblem { title: string; currentETag: string; current: InstitutionUser; }

const versioned = (res: HttpResponse<InstitutionUser>): Versioned<InstitutionUser> =>
  ({ value: res.body!, etag: res.headers.get('ETag')! });

@Injectable({ providedIn: 'root' })
export class InstitutionUserApi {
  private readonly http = inject(HttpClient);

  load(id: number): Observable<Versioned<InstitutionUser>> {
    return this.http.get<InstitutionUser>(`/api/institution-users/${id}`, { observe: 'response' }).pipe(map(versioned));
  }

  save(id: number, changes: InstitutionUserChanges, etag: string): Observable<Versioned<InstitutionUser>> {
    return this.http
      .put<InstitutionUser>(`/api/institution-users/${id}`, changes, { observe: 'response', headers: { 'If-Match': etag } })
      .pipe(map(versioned));
  }
}

export class InstitutionUserEditor {
  private readonly api = inject(InstitutionUserApi);
  base!: Versioned<InstitutionUser>; // the version when the form was opened
  readonly conflicts = signal<(keyof InstitutionUserChanges)[]>([]);

  save(mine: InstitutionUserChanges, onMerged: (merged: InstitutionUserChanges) => void): void {
    this.api.save(this.base.value.id, mine, this.base.etag).subscribe({
      next: saved => (this.base = saved),
      error: (e: HttpErrorResponse) => {
        if (e.status !== 412) {
          throw e;
        }
        const problem = e.error as ConflictProblem;
        const { id, ...baseChanges } = this.base.value;
        const { id: _, ...theirs } = problem.current;
        const result = threeWayMerge<InstitutionUserChanges>(baseChanges, mine, theirs);
        // new base: the current version on the server; the user sees the merged form and saves again
        this.base = { value: problem.current, etag: problem.currentETag };
        this.conflicts.set(result.conflicts);
        onMerged(result.merged);
      }
    });
  }
}
