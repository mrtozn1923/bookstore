import { concatMap, defer, finalize, map, mergeMap, range, timer, type OperatorFunction } from "rxjs";
import { log, runInVirtualTime } from "../../tools/virtual-time.ts";

let active = 0;
let maxActive = 0;

// loading one cover image takes 100 ms; active/maxActive count parallel downloads
function loadCover(index: number) {
  return defer(() => {
    active++;
    maxActive = Math.max(maxActive, active);
    return timer(100).pipe(
      map(() => `cover-${index}.jpg`),
      finalize(() => active--),
    );
  });
}

function runCovers(name: string, flatten: OperatorFunction<number, string>) {
  active = 0;
  maxActive = 0;
  runInVirtualTime(name, () => {
    let loaded = 0;
    range(1, 50)
      .pipe(flatten)
      .subscribe({
        next: () => loaded++,
        complete: () => log(`${loaded} covers loaded, at most ${maxActive} at the same time`),
      });
  });
}

runCovers("mergeMap (no limit)", mergeMap((i) => loadCover(i)));
runCovers("mergeMap(..., 4)", mergeMap((i) => loadCover(i), 4));
runCovers("concatMap", concatMap((i) => loadCover(i)));
