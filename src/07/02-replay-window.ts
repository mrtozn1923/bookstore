import { ReplaySubject, timer } from "rxjs";
import { log, runInVirtualTime } from "../../tools/virtual-time.ts";

runInVirtualTime("ReplaySubject(3, 10000): recently viewed books", () => {
  // keep at most 3 views, and only those from the last 10 seconds
  const viewed = new ReplaySubject<string>(3, 10_000);
  const views: [number, string][] = [[0, "Dune"], [2000, "Emma"], [4000, "The Hobbit"], [6000, "Middlemarch"], [13000, "Frankenstein"]];
  for (const [ms, title] of views) timer(ms).subscribe(() => viewed.next(title));

  for (const ms of [7000, 15000]) {
    timer(ms).subscribe(() => {
      const replayed: string[] = [];
      viewed.subscribe((title) => replayed.push(title)).unsubscribe();
      log(`late subscriber gets: ${replayed.join(", ")}`);
    });
  }
});
