// An old CommonJS module whose exports are built at runtime
function createPricing(vatRate) {
    return {
        withVat: (amount) => Math.round(amount * (1 + vatRate) * 100) / 100,
    };
}

module.exports = createPricing(0.1);
