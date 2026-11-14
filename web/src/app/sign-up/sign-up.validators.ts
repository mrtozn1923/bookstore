import { AbstractControl, AsyncValidatorFn, ValidationErrors } from '@angular/forms';
import { Observable, of } from 'rxjs';
import { delay, map } from 'rxjs/operators';

export function noWhitespace(control: AbstractControl): ValidationErrors | null {
  const value = (control.value || '') as string;
  return value.includes(' ') ? { whitespace: true } : null;
}

const takenUsernames = ['admin', 'root', 'bookstore'];

export function usernameAvailable(): AsyncValidatorFn {
  return (control: AbstractControl): Observable<ValidationErrors | null> =>
    of(takenUsernames.includes(control.value)).pipe(
      delay(500),
      map(taken => (taken ? { usernameTaken: true } : null))
    );
}
