export interface Observer<T> {
  next: (value: T) => void;
  error: (err: unknown) => void;
  complete: () => void;
}
type Teardown = (() => void) | void;
type Operator<A, B> = (source: MiniObservable<A>) => MiniObservable<B>;

export class MiniObservable<T> {
  readonly producer: (subscriber: Observer<T>) => Teardown;
  constructor(producer: (subscriber: Observer<T>) => Teardown) {
    this.producer = producer;
  }

  subscribe(partial: Partial<Observer<T>>): { unsubscribe: () => void } {
    let closed = false;
    let teardown: Teardown = undefined;
    const close = () => {
      if (closed) return;
      closed = true;
      teardown?.();
    };
    const subscriber: Observer<T> = {
      next: (v) => { if (!closed) partial.next?.(v); },
      error: (e) => { if (!closed) { partial.error?.(e); close(); } },
      complete: () => { if (!closed) { partial.complete?.(); close(); } },
    };
    try {
      teardown = this.producer(subscriber);
    } catch (err) {
      subscriber.error(err);
    }
    if (closed) teardown?.(); // the producer finished synchronously
    return { unsubscribe: close };
  }

  pipe(...operators: Operator<any, any>[]): MiniObservable<any> {
    return operators.reduce((source, op) => op(source), this as MiniObservable<any>);
  }
}

export const map = <A, B>(fn: (v: A) => B): Operator<A, B> => (source) =>
  new MiniObservable<B>((sub) => source.subscribe({ ...sub, next: (v) => sub.next(fn(v)) }).unsubscribe);

export const filter = <A>(fn: (v: A) => boolean): Operator<A, A> => (source) =>
  new MiniObservable<A>((sub) => source.subscribe({ ...sub, next: (v) => fn(v) && sub.next(v) }).unsubscribe);
