// Six requests, each claiming a different client IP in X-Forwarded-For; limit is 3 per window
import express from "express";
import { rateLimit } from "express-rate-limit";

for (const setting of [0, 1, true]) {
    const app = express();
    app.set("trust proxy", setting);
    app.use(rateLimit({ windowMs: 60_000, limit: 3, standardHeaders: "draft-8", legacyHeaders: false }));
    app.get("/", (req, res) => res.json({ ip: req.ip }));

    const server = app.listen(0, "127.0.0.1");
    await new Promise((resolve) => server.once("listening", resolve));
    const { port } = server.address() as { port: number };
    const statuses: number[] = [];
    for (let i = 1; i <= 6; i++) {
        const res = await fetch(`http://127.0.0.1:${port}/`, { headers: { "x-forwarded-for": `203.0.113.${i}` } });
        statuses.push(res.status);
    }
    console.log(`trust proxy = ${String(setting).padEnd(4)} -> ${statuses.join(" ")}`);
    server.close();
}
