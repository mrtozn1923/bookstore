import { ErrorHandler, Injectable } from '@angular/core';

// A stand-in for an error tracking SDK: one JSON line per error, with the minified stack
@Injectable()
export class BookstoreErrorHandler implements ErrorHandler {
  handleError(error: unknown): void {
    const report = {
      message: error instanceof Error ? error.message : String(error),
      stack: error instanceof Error ? error.stack : undefined,
      url: location.href,
    };
    console.error(`[bookstore-error] ${JSON.stringify(report)}`);
  }
}
