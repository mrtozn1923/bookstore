const cartLine = { isbn: "9780441172719", quantity: 2, giftWrap: false };

type CartLine = typeof cartLine;
type CartLineKey = keyof CartLine;

const key: CartLineKey = "price";
