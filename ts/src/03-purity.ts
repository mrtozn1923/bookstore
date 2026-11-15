import { computed, signal } from "@angular/core";

const campaignEnd = new Date("2026-12-01T00:00:00+03:00");

// Pure: everything it needs comes in as a parameter
function isCouponActive(code: string, now: Date): boolean {
  return code === "AUTUMN10" && now < campaignEnd;
}

// Impure computed: reads a plain variable that Angular can't track
let now = new Date("2026-11-30T23:59:00+03:00");
const coupon = signal("AUTUMN10");
const activeImpure = computed(() => isCouponActive(coupon(), now));

// Pure computed: the clock is a signal too
const clock = signal(new Date("2026-11-30T23:59:00+03:00"));
const activePure = computed(() => isCouponActive(coupon(), clock()));

console.log("23:59 ->", "impure:", activeImpure(), "pure:", activePure());

now = new Date("2026-12-01T00:01:00+03:00");
clock.set(new Date("2026-12-01T00:01:00+03:00"));
console.log("00:01 ->", "impure:", activeImpure(), "pure:", activePure());
