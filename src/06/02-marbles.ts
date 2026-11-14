import { asyncScheduler, auditTime, bufferTime, debounceTime, delay, sampleTime, throttleTime } from "rxjs";
import { showMarbles } from "../../tools/marble.ts";

// one character = 1 ms of virtual time: a quick burst (A B C), a pause, then a value every 3 ms
const keys = "ABC------D--E--F--G---|";

showMarbles("debounceTime(4)", (cold) => cold(keys).pipe(debounceTime(4)), 24);
showMarbles("throttleTime(4)", (cold) => cold(keys).pipe(throttleTime(4)), 24);
showMarbles("throttleTime(4) trailing", (cold) => cold(keys).pipe(throttleTime(4, asyncScheduler, { leading: false, trailing: true })), 24);
showMarbles("auditTime(4)", (cold) => cold(keys).pipe(auditTime(4)), 24);
showMarbles("sampleTime(5)", (cold) => cold(keys).pipe(sampleTime(5)), 24);
showMarbles("delay(2)", (cold) => cold(keys).pipe(delay(2)), 24);
showMarbles("bufferTime(5)", (cold) => cold(keys).pipe(bufferTime(5)), 24);
