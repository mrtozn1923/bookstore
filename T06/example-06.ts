// Error: Property 'warehouse' of type 'string' is not assignable to 'string' index type 'number'.
function report(data: { [isbn: string]: number; warehouse: string }) {
  console.log(data.warehouse);
}
