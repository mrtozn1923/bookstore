class CartLine {
  // Error (with erasableSyntaxOnly on): This syntax is not allowed when 'erasableSyntaxOnly' is enabled.
  constructor(private isbn: string, private quantity: number) {}
}
