import { ColdObservable } from "rxjs";

// a fake GET /api/categories: logs when the request starts and when its teardown runs
function categoriesProducer(subscriber: Subscriber<string>) {
  console.log("-> GET /api/categories");
  const handle = setTimeout(() => {
    subscriber.next("classics, fantasy");
    subscriber.complete();
  }, 200);
  subscriber.addTeardown(() => {
    clearTimeout(handle);
    console.log("   teardown");
  });
}

async function twoSubscribers(title: string, source: Observable<string>) {
  console.log(`--- ${title}`);
  const nav = new AbortController();
  const select = new AbortController();
  source.subscribe((list) => console.log(`nav: ${list}`), { signal: nav.signal });
  source.subscribe((list) => console.log(`select: ${list}`), { signal: select.signal });
  await new Promise((resolve) => setTimeout(resolve, 300));
}

await twoSubscribers("platform Observable", new Observable(categoriesProducer));
await twoSubscribers("ColdObservable", new ColdObservable(categoriesProducer));

console.log("--- abort before the answer");
const source = new Observable(categoriesProducer);
const nav = new AbortController();
const select = new AbortController();
source.subscribe((list) => console.log(`nav: ${list}`), { signal: nav.signal });
source.subscribe((list) => console.log(`select: ${list}`), { signal: select.signal });
nav.abort();
console.log("nav aborted");
select.abort();
console.log("select aborted");
