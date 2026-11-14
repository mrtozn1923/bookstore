// I15: examples that need a browser; paste these blocks into the DevTools console one by one
// (if a block uses a function from examples.js, paste that file first)

// --- example 1 ---
class StockBadge extends HTMLElement {
  static observedAttributes = ["count"];

  constructor() {
    super();
    console.log("1 constructor");
  }
  connectedCallback() {
    console.log("2 connected");
  }
  attributeChangedCallback(name, oldValue, newValue) {
    console.log("3 attribute", name, oldValue, newValue);
  }
}

customElements.define("stock-badge", StockBadge);

const badge = document.createElement("stock-badge");
badge.setAttribute("count", "12");
document.body.append(badge);

// --- example 9 ---
class CounterBox extends HTMLElement {
  connectedCallback() { console.log("connected"); }
  disconnectedCallback() { console.log("disconnected"); }
}
customElements.define("counter-box", CounterBox);

const a = document.createElement("counter-box");
const b = document.createElement("div");
document.body.append(a);
b.append(a);
document.body.append(b);
