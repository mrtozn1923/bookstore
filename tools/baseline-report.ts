// 10: the Baseline status of the browser features this series uses, read from web-features
// Run: npm run baseline
import { features } from "web-features";

// [web-features id, post, the compat keys we really call (when the feature is larger than what we use)]
const used: [string, string, string[]?][] = [
  ["abortsignal-any", "01"],
  ["fetch-request-streams", "01"],
  ["fetchlater", "01"],
  ["cookie-store", "02", ["api.Window.cookieStore", "api.CookieStore.get", "api.CookieStore.set"]],
  ["origin-private-file-system", "02"],
  ["storage-buckets", "02"],
  ["indexeddb", "03"],
  ["service-workers", "04"],
  ["manifest", "06"],
  ["beforeinstallprompt", "06"],
  ["navigator-install", "06"],
  ["badging", "06"],
  ["push", "07"],
  ["background-sync", "07"],
  ["scheduler", "08"],
  ["shared-workers", "08"],
  ["event-timing", "08"],
  ["web-locks", "09"],
  ["broadcast-channel", "09"],
  ["navigation", "09"],
];

type Status = { baseline: "high" | "low" | false; baseline_low_date?: string; baseline_high_date?: string; support: Record<string, string> };

function describe(status: Status): string {
  if (status.baseline === "high") return `widely available since ${status.baseline_high_date}`;
  if (status.baseline === "low") return `newly available since ${status.baseline_low_date}`;
  const engines = Object.keys(status.support).filter((b) => !b.endsWith("_android") && !b.endsWith("_ios"));
  return `limited: ${engines.join(", ") || "no stable browser"}`;
}

for (const [id, post, keys] of used) {
  const feature = features[id] as { kind: string; status: Status & { by_compat_key?: Record<string, Status> } };
  console.log(`${post} ${id.padEnd(27)} ${describe(feature.status)}`);
  for (const key of keys ?? []) console.log(`     ${key.padEnd(30)} ${describe(feature.status.by_compat_key![key])}`);
}
