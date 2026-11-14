type Json = string | number | boolean | null | Json[] | { [key: string]: Json };

const book: Json = { title: "Dune", tags: ["classic"], stock: { ankara: 12 } };
const order: Json = { placedAt: new Date() };
const cartLine: Json = { giftNote: undefined };
