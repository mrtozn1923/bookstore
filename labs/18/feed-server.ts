import http from "node:http";
import { Readable } from "node:stream";

// A supplier's price feed: ~50 MB of CSV, produced line by line
function* feedLines(): Generator<string> {
    yield "isbn;price;currency\n";
    for (let i = 0; i < 2_000_000; i++) {
        yield `978${String(i).padStart(10, "0")};${(100 + (i % 400)).toFixed(2)};TRY\n`;
    }
}

http.createServer((req, res) => {
    res.writeHead(200, { "content-type": "text/csv" });
    Readable.from(feedLines()).pipe(res);
}).listen(52183, "127.0.0.1", () => console.log("feed on 52183"));
