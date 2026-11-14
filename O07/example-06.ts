type StoreConfig = {
  apiUrl: string;
  features: { giftWrap: boolean };
};

const config: Readonly<StoreConfig> = {
  apiUrl: "https://api.example.com",
  features: { giftWrap: false },
};

// Error: Cannot assign to 'apiUrl' because it is a read-only property.
config.apiUrl = "https://other.example.com";

config.features.giftWrap = true; // compiles!

const currencies: Readonly<string[]> = ["TRY", "EUR"];
// Error: Property 'push' does not exist on type 'readonly string[]'.
currencies.push("USD");
