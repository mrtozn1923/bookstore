// ESM with top-level await: loads the rates before anyone can use the module
import { setTimeout as delay } from "node:timers/promises";

await delay(10);
export const rates = { EUR: 37.6 };
