// /api isteklerini E2E host'una yönlendirir; adres playwright.config.ts'teki webServer'dan gelir
export default {
  '/api': {
    target: process.env.BOOKSTORE_API_URL ?? 'http://127.0.0.1:5280',
    secure: false,
  },
};
