import express, { type NextFunction, type Request, type Response } from "express";

const app = express();

app.use((req, res, next) => {
    console.log("1. global middleware");
    next();
});

const books = express.Router();
books.use((req, res, next) => {
    console.log("2. router middleware");
    next();
});
books.get("/:isbn", async (req, res) => {
    console.log("3. handler, about to throw");
    throw new Error(`stock service down for ${req.params.isbn}`);
});
app.use("/api/books", books);

// Looks like an error handler, but has three parameters: Express treats it as normal middleware
app.use((err: any, req: any, res: any) => {
    console.log("never called");
    res.status(500).json({ error: "from the 3-parameter handler" });
});

// Four parameters: this one receives the error
app.use((err: Error, req: Request, res: Response, next: NextFunction) => {
    console.log(`4. error middleware: ${err.message}`);
    res.status(503).json({ error: err.message });
});

const server = app.listen(0, "127.0.0.1", async () => {
    const { port } = server.address() as { port: number };
    const res = await fetch(`http://127.0.0.1:${port}/api/books/9780441172719`);
    console.log(`client got ${res.status} ${await res.text()}`);
    server.close();
});
