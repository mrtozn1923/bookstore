document.querySelector("#show-report").addEventListener("click", async () => {
  const { drawSalesReport } = await import("./sales-report.js");
  drawSalesReport(document.querySelector("#report"));
});

// Picking a translation file based on language
const lang = navigator.language.startsWith("tr") ? "tr" : "en";
const { default: messages } = await import(`./i18n/${lang}.js`);
console.log(messages.welcome);
