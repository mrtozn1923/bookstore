// Prints the parts of the Node.js runtime and their versions
import { builtinModules } from "node:module";

const { node, v8, uv, openssl, napi, modules } = process.versions;
console.log({ node, v8, uv, openssl, napi, modules });
console.log("lts:", process.release.lts ?? "(not an LTS release)");
console.log("built-in modules:", builtinModules.filter((m) => !m.startsWith("_")).length);
console.log(builtinModules.filter((m) => ["fs", "http", "crypto", "worker_threads"].includes(m)));
