import express from "express";

// What does NODE_ENV change inside an Express app?
const app = express();
console.log({ env: app.get("env"), viewCache: app.enabled("view cache"), etag: app.get("etag"), xPoweredBy: app.enabled("x-powered-by") });
