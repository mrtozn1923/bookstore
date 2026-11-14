type ShippingOptions = { [K in "giftWrap" | "express"]: boolean };

const options: ShippingOptions = { giftWrap: true, express: false };
