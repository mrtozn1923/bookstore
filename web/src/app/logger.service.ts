import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class LoggerService {
  log(message: string): void {
    console.log(message);
  }
}

@Injectable()
export class SilentLoggerService extends LoggerService {
  log(message: string): void { }
}
