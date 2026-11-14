import { average } from "./math.js";

self.onmessage = (e) => {
  self.postMessage(average(e.data));
};
