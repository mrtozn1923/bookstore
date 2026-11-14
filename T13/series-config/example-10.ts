// in book.ts: export interface Book { title: string }

// Error: 'Book' is a type and must be imported using a type-only import when 'verbatimModuleSyntax' is enabled.
import { Book } from "./book.ts";

// Error: This syntax is not allowed when 'erasableSyntaxOnly' is enabled.
enum OrderStatus {
  Pending,
  Shipped,
}
