type CartAction =
  | { type: "add"; isbn: string }
  | { type: "remove"; lineId: number }
  | { type: "clear" };

type PayloadAction = Exclude<CartAction, { type: "clear" }>;
type PayloadType = PayloadAction["type"]; // "add" | "remove"

function logPayload(action: PayloadAction) {
  if (action.type === "add") {
    console.log(`add ${action.isbn}`);
  } else {
    console.log(`remove line ${action.lineId}`);
  }
}

logPayload({ type: "add", isbn: "9780441172719" }); // add 9780441172719
