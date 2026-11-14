import { Subject, type Observable } from "rxjs";

class LeakyCartEvents {
  readonly added$ = new Subject<string>(); // anyone can call next
}

class CartEvents {
  readonly #added = new Subject<string>();
  readonly added$: Observable<string> = this.#added.asObservable(); // read-only view

  notifyAdded(title: string): void {
    this.#added.next(title);
  }
}

const leaky = new LeakyCartEvents();
leaky.added$.subscribe((title) => console.log(`toast: ${title} added to cart`));
leaky.added$.next("Free Bookmark"); // a component far away "helps" and fakes an event

const events = new CartEvents();
events.added$.subscribe((title) => console.log(`toast: ${title} added to cart`));
events.notifyAdded("Dune");
console.log("added$ has next:", "next" in events.added$);
