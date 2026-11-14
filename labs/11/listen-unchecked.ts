import express from "express";

// The Express 4 habit: the callback ignores its argument
const app = express();
app.listen(3000, "127.0.0.1", () => console.log("listening on 3000"));
