import { Component, inject, ChangeDetectionStrategy } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormArray, FormControl, NonNullableFormBuilder, Validators, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { debounceTime } from 'rxjs/operators';
import { noWhitespace, usernameAvailable } from './sign-up.validators';


@Component({
    selector: 'app-sign-up',
    templateUrl: './sign-up.component.html',
    changeDetection: ChangeDetectionStrategy.Eager,
    imports: [FormsModule, ReactiveFormsModule]
})
export class SignUpComponent {
  private readonly fb = inject(NonNullableFormBuilder);

  signUpForm = this.fb.group({
    fullName: ['', [Validators.required, Validators.minLength(3)]],
    email: ['', [Validators.required, Validators.email]],
    username: ['', [Validators.required, noWhitespace], [usernameAvailable()]],
    genres: this.fb.array<string>([])
  });

  get genres(): FormArray<FormControl<string>> {
    return this.signUpForm.controls.genres;
  }

  constructor() {
    this.signUpForm.valueChanges
      .pipe(debounceTime(500), takeUntilDestroyed())
      .subscribe(value => {
        localStorage.setItem('signUpDraft', JSON.stringify(value));
      });
  }

  addGenre(): void {
    this.genres.push(this.fb.control('', Validators.required));
  }

  removeGenre(index: number): void {
    this.genres.removeAt(index);
  }

  onSubmit(): void {
    if (this.signUpForm.invalid) {
      this.signUpForm.markAllAsTouched();
      return;
    }
    console.log(this.signUpForm.value);
  }
}
