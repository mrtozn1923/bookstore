class OrderEmail {
  subject(): string {
    return "...";
  }
}

class ShippedEmail extends OrderEmail {
  override subject(): string {
    return "Your order has shipped";
  }
}

class DeliveredEmail extends OrderEmail {
  // Error: This member cannot have an 'override' modifier because it is not declared in the base class 'OrderEmail'. Did you mean 'subject'?
  override subjet(): string {
    return "Your order has been delivered";
  }
}
