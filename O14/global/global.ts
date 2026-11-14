interface StoreConfig {
  apiUrl: string;
  currency: "TRY" | "EUR";
}

declare global {
  interface Window {
    storeConfig: StoreConfig;
  }
}

export {};
