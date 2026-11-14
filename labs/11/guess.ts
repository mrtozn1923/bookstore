import express from "express";

const app = express();

// The route is registered first, the JSON parser after it
app.post("/api/orders", async (req, res) => {
    const { customerId } = req.body;
    res.status(201).json({ customerId });
});
app.use(express.json());

app.listen(3101, "127.0.0.1", () => console.log("express 5 on 3101"));
