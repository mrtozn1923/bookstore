import { Service, signal } from '@angular/core';

@Service()
export class ServerStatus {
  readonly #unavailable = signal(false);
  readonly unavailable = this.#unavailable.asReadonly();

  report(unavailable: boolean): void {
    this.#unavailable.set(unavailable);
  }
}
