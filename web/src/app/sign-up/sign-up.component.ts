import { Component, OnDestroy, OnInit, inject } from '@angular/core';
import { FormArray, FormControl, NonNullableFormBuilder, Validators } from '@angular/forms';
import { Subject } from 'rxjs';
import { debounceTime, takeUntil } from 'rxjs/operators';
import { noWhitespace, usernameAvailable } from './sign-up.validators';

@Component({
  selector: 'app-sign-up',
  templateUrl: './sign-up.component.html'
})
export class SignUpComponent implements OnInit, OnDestroy {
  private readonly fb = inject(NonNullableFormBuilder);

  signUpForm = this.fb.group({
    fullName: ['', [Validators.required, Validators.minLength(3)]],
    email: ['', [Validators.required, Validators.email]],
    username: ['', [Validators.required, noWhitespace], [usernameAvailable()]],
    genres: this.fb.array<string>([])
  });

  private destroy$ = new Subject<void>();

  get genres(): FormArray<FormControl<string>> {
    return this.signUpForm.controls.genres;
  }

  ngOnInit(): void {
    this.signUpForm.valueChanges
      .pipe(debounceTime(500), takeUntil(this.destroy$))
      .subscribe(value => {
        localStorage.setItem('signUpDraft', JSON.stringify(value));
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
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
