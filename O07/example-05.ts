type ListOptions = {
  page?: number;
  pageSize?: number;
};

const defaults: Required<ListOptions> = { page: 1, pageSize: 20 };

function resolveOptions(options: ListOptions): Required<ListOptions> {
  return { ...defaults, ...options };
}

console.log(resolveOptions({ pageSize: 50 })); // { page: 1, pageSize: 50 }

// Error: Property 'pageSize' is missing in type '{ page: number; }' but required in type 'Required<ListOptions>'.
const incomplete: Required<ListOptions> = { page: 2 };
