// WRONG: origin: true reflects any Origin, and with credentials: true the browser sends cookies along
import express from "express";
import cors from "cors";

const app = express();
app.use(cors({ origin: true, credentials: true }));
app.get("/api/orders/mine", (req, res) => res.json([{ id: "ORD-1001" }]));

const server = app.listen(0, "127.0.0.1", async () => {
    const { port } = server.address() as { port: number };
    const res = await fetch(`http://127.0.0.1:${port}/api/orders/mine`, { headers: { origin: "https://evil.example" } });
    console.log("access-control-allow-origin:", res.headers.get("access-control-allow-origin"));
    console.log("access-control-allow-credentials:", res.headers.get("access-control-allow-credentials"));
    server.close();
});
