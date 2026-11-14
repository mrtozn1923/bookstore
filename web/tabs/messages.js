// BroadcastChannel vs. the storage event: who gets the message, and when?
const channel = new BroadcastChannel("bookstore-lab");
export const received = [];

export function listen() {
  channel.onmessage = (event) => received.push(`BroadcastChannel ${JSON.stringify(event.data)}`);
  addEventListener("storage", (event) => received.push(`storage key=${event.key} newValue=${event.newValue}`));
}

export function announce(text) {
  channel.postMessage({ type: "signed-out", text });
  localStorage.setItem("lab-signal", text); // other tabs get a storage event; this tab does not
  localStorage.setItem("lab-signal", text); // same value again: no event at all
}
