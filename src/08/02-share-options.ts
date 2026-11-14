import { ReplaySubject, share, shareReplay, timer, type Observable } from "rxjs";
import { log, runInVirtualTime } from "../../tools/virtual-time.ts";
import { fakeRequest } from "../04/fake-api.ts";

const getCategories = () => fakeRequest("GET /categories", 200, () => ["science-fiction", "classics", "fantasy"]);

// three parts of the page subscribe at 0 ms, 0 ms and 1000 ms (and a fourth at 90 s)
function subscribeLikeAPage(categories$: Observable<string[]>, times = [0, 0, 1000]) {
  times.forEach((ms, i) => timer(ms).subscribe(() => categories$.subscribe((c) => log(`subscriber ${i + 1}: ${c.length} categories`))));
}

runInVirtualTime("no sharing", () => subscribeLikeAPage(getCategories()));
runInVirtualTime("share()", () => subscribeLikeAPage(getCategories().pipe(share())));
runInVirtualTime("shareReplay(1)", () => subscribeLikeAPage(getCategories().pipe(shareReplay(1))));
runInVirtualTime("share as a 60-second cache", () =>
  subscribeLikeAPage(
    getCategories().pipe(
      share({
        connector: () => new ReplaySubject(1),
        resetOnComplete: () => timer(60_000), // forget the answer 60 s after it arrived
        resetOnError: true,
        resetOnRefCountZero: false,
      }),
    ),
    [0, 0, 1000, 90_000],
  ),
  120_000, // let the virtual clock run past 90 s
);
