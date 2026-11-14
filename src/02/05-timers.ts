import { interval, take, timer } from "rxjs";

const start = performance.now();
const at = () => `${Math.round((performance.now() - start) / 100) * 100} ms`;

// timer(due): one value (0) after the delay, then complete
timer(300).subscribe({
  next: (v) => console.log(at(), "timer(300):", v),
  complete: () => console.log(at(), "timer(300): complete"),
});

// timer(0, period): first value right away, then every period
timer(0, 200).pipe(take(3)).subscribe((v) => console.log(at(), "timer(0, 200):", v));

// interval(period): first value only after one period
interval(200).pipe(take(3)).subscribe((v) => console.log(at(), "interval(200):", v));
