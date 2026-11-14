import express from "express";

const app = express();

// Express 5 / path-to-regexp 8: wildcards need a name, optional parts use braces
app.get("/covers/*file", (req, res) => res.json({ route: "/covers/*file", params: req.params }));
app.get("/api/books/:isbn{.:format}", (req, res) => res.json({ route: "/api/books/:isbn{.:format}", params: req.params }));
app.get("/api/authors{/:id}", (req, res) => res.json({ route: "/api/authors{/:id}", params: req.params }));

// Express 4 syntax that no longer parses
for (const old of ["/covers/*", "/api/books/:isbn?", "/api/books/:isbn(\\d+)"]) {
    try {
        app.get(old, (req, res) => res.end());
        console.log(`${old.padEnd(24)} accepted`);
    } catch (err) {
        console.log(`${old.padEnd(24)} ${(err as Error).message.split(";")[0]}`);
    }
}

const server = app.listen(0, "127.0.0.1", async () => {
    const { port } = server.address() as { port: number };
    for (const path of ["/covers/2026/9780441172719.jpg", "/api/books/9780441172719.json", "/api/books/9780441172719", "/api/authors", "/api/authors/42"]) {
        const body = await (await fetch(`http://127.0.0.1:${port}${path}`)).json();
        console.log(`${path.padEnd(32)} -> ${JSON.stringify(body.params)}`);
    }
    server.close();
});
