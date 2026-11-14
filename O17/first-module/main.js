import { orderTotal } from "./pricing.js";

document.querySelector("#result").textContent =
  `Dune + Emma with shipping: ${orderTotal([450, 320])} TRY`;
