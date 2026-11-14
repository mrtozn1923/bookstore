// A promise rejects and nobody awaits it
async function chargeCard(orderId: string): Promise<void> {
    throw new Error(`payment gateway timeout for ${orderId}`);
}

chargeCard("ORD-1003");
setTimeout(() => console.log("still serving requests?"), 10);
