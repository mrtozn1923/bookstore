const cartLine = { isbn: "9780441172719", quantity: 2, giftWrap: false };

for (const key of Object.keys(cartLine)) {
  // Error: Element implicitly has an 'any' type because expression of type 'string' can't be used to index type '{ isbn: string; quantity: number; giftWrap: boolean; }'.
  console.log(cartLine[key]);
}
