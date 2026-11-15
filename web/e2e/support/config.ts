// Yerelde varsayılan portlar; CI ya da aynı makinede ikinci bir kopya ortam değişkeniyle değiştirir
export const apiPort = Number(process.env.BOOKSTORE_API_PORT ?? 5280);
export const webPort = Number(process.env.BOOKSTORE_WEB_PORT ?? 4200);
export const apiURL = process.env.BOOKSTORE_API_URL ?? `http://127.0.0.1:${apiPort}`;
export const webURL = process.env.BOOKSTORE_WEB_URL ?? `http://localhost:${webPort}`;
export const customerStorageState = 'playwright/.auth/customer.json';
export const buyerStorageState = 'playwright/.auth/buyer.json';
export const timezoneId = 'Europe/Istanbul';
