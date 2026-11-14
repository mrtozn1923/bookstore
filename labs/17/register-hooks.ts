import { register } from "node:module";

// Async customization hooks via module.register(): runtime-deprecated in Node 26
register("data:text/javascript,export async function resolve(s, c, next) { return next(s, c); }", import.meta.url);
console.log("hooks registered");
