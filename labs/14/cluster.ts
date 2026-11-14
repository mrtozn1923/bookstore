import cluster from "node:cluster";
import http from "node:http";

const workers = 4;

if (cluster.isPrimary) {
    // The primary only manages workers; it accepts connections and hands them out (round-robin outside Windows)
    console.log(`primary ${process.pid}, scheduling ${cluster.schedulingPolicy === cluster.SCHED_RR ? "round-robin" : "by the OS"}`);
    for (let i = 0; i < workers; i++) {
        cluster.fork();
    }
    let listening = 0;
    cluster.on("listening", async () => {
        if (++listening < workers) {
            return;
        }
        const answeredBy = new Map<string, number>();
        for (let i = 0; i < 8; i++) {
            // agent: false -> a new connection per request, so every request is distributed again
            const pid = await new Promise<string>((resolve) => http.get({ host: "127.0.0.1", port: 3002, agent: false }, (res) => res.setEncoding("utf8").on("data", resolve)));
            answeredBy.set(pid, (answeredBy.get(pid) ?? 0) + 1);
        }
        console.log("requests per worker:", Object.fromEntries(answeredBy));
        cluster.disconnect();
    });
} else {
    // Every worker is a separate process with its own event loop, heap and SQL pool
    http.createServer((req, res) => res.end(String(process.pid))).listen(3002, "127.0.0.1");
}
