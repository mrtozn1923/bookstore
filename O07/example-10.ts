type Customer = { id: number; name: string; email: string };

type WithoutEmail = Omit<Customer, "emial">; // compiles, email is still there

type ListOptions = { pageSize?: number };
const defaults: Required<ListOptions> = { pageSize: 20 };

function resolve(options: ListOptions): Required<ListOptions> {
  return { ...defaults, ...options };
}

console.log(resolve({ pageSize: undefined })); // { pageSize: undefined }
