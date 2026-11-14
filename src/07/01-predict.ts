import { AsyncSubject, BehaviorSubject, ReplaySubject, Subject } from "rxjs";

// Dune's stock goes 12 -> 11 -> 10 -> 9; a late subscriber joins after 10
const subjects = {
  Subject: new Subject<number>(),
  BehaviorSubject: new BehaviorSubject<number>(12),
  "ReplaySubject(2)": new ReplaySubject<number>(2),
  AsyncSubject: new AsyncSubject<number>(),
};

for (const [name, subject] of Object.entries(subjects)) {
  const seen: string[] = [];
  subject.next(11);
  subject.next(10);
  subject.subscribe({ next: (stock) => seen.push(String(stock)), complete: () => seen.push("|") });
  subject.next(9);
  subject.complete();
  console.log(`${name.padEnd(17)} late subscriber saw: ${seen.join(" ")}`);
}
