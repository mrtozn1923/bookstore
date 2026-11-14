// What an upgrade changes under the hood: engine, libuv, HTTP client and native addon ABI
const { node, v8, uv, undici, modules, napi } = process.versions;
console.log({ node, v8, uv, undici, modules, napi, lts: process.release.lts });
