import { log, runInVirtualTime } from "../../tools/virtual-time.ts";
import { isRetryable, seededRandom } from "../09/backoff.ts";
import { flakyHttp } from "../09/fake-http.ts";
import { loadingState, retryWithBackoff, type LoadingState } from "./operators.ts";

const show = (state: LoadingState<string[]>) =>
  log(
    state.status === "success" ? `success: ${state.data.join(", ")}`
    : state.status === "error" ? `error: ${String(state.error)}`
    : "loading",
  );

runInVirtualTime("categories: one 503, then 200", () => {
  flakyHttp("GET /categories", 200, [503, 200], () => ["classics", "fantasy"])
    .pipe(retryWithBackoff({ count: 2, isRetryable, random: seededRandom(42) }), loadingState())
    .subscribe(show);
});

runInVirtualTime("categories: 503 every time", () => {
  flakyHttp("GET /categories", 200, [503], () => ["classics"])
    .pipe(retryWithBackoff({ count: 2, isRetryable, random: seededRandom(42) }), loadingState())
    .subscribe(show);
});

runInVirtualTime("wrong order: loadingState before retryWithBackoff", () => {
  flakyHttp("GET /categories", 200, [503, 200], () => ["classics", "fantasy"])
    .pipe(loadingState(), retryWithBackoff({ count: 2, isRetryable, random: seededRandom(42) }))
    .subscribe(show);
});
