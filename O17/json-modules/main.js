import config from "./config.json" with { type: "json" };
console.log(config.pageSize); // 20

// The dynamic form
const { default: settings } = await import("./config.json", {
  with: { type: "json" }
});
