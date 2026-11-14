class CatalogClient {
  baseUrl: string;
  timeoutMs: number;

  constructor(baseUrl: string, timeoutMs: number) {
    this.baseUrl = baseUrl;
    this.timeoutMs = timeoutMs;
  }
}

type ClientArgs = ConstructorParameters<typeof CatalogClient>;
// [baseUrl: string, timeoutMs: number]

type Client = InstanceType<typeof CatalogClient>; // CatalogClient

function create<T extends new (...args: any) => any>(
  ctor: T,
  ...args: ConstructorParameters<T>
): InstanceType<T> {
  return new ctor(...args);
}

const client = create(CatalogClient, "https://api.example.com", 3000);
console.log(client.timeoutMs); // 3000

// Error: Expected 3 arguments, but got 2.
create(CatalogClient, "https://api.example.com");

// Error: Type 'CatalogClient' does not satisfy the constraint 'abstract new (...args: any) => any'.
type WrongClient = InstanceType<CatalogClient>;
