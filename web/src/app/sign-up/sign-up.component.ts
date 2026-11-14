import { Component, OnDestroy, OnInit } from '@angular/core';
import { FormArray, FormBuilder, Validators } from '@angular/forms';
import { Subject } from 'rxjs';
import { debounceTime, takeUntil } from 'rxjs/operators';
import { noWhitespace, usernameAvailable } from './sign-up.validators';

@Component({
  selector: 'app-sign-up',
  templateUrl: './sign-up.component.html'
})
export class SignUpComponent implements OnInit, OnDestroy {
  signUpForm = this.fb.group({
    fullName: ['', [Validators.required, Validators.minLength(3)]],
    email: ['', [Validators.required, Validators.email]],
    username: ['', [Validators.required, noWhitespace], [usernameAvailable()]],
    genres: this.fb.array([])
  });

  private destroy$ = new Subject<void>();

  constructor(private fb: FormBuilder) { }

  get genres(): FormArray {
    return this.signUpForm.get('genres') as FormArray;
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
