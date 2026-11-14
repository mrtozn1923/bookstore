// Error: This syntax is not allowed when 'erasableSyntaxOnly' is enabled.
enum OrderStatus {
  Pending,
  Paid,
}

// Error: This syntax is not allowed when 'erasableSyntaxOnly' is enabled.
namespace Shipping {
  export const FREE_SHIPPING_OVER = 500;
}

class CartLine {
  // Error: This syntax is not allowed when 'erasableSyntaxOnly' is enabled.
  constructor(public readonly isbn: string) {}
}

// Fine: a namespace containing only types can be erased
namespace Catalog {
  export interface BookSummary {
    isbn: string;
    title: string;
  }
}
