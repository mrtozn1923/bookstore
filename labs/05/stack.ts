interface OrderItem {
    isbn: string;
    quantity: number;
}

function checkQuantity(item: OrderItem): void {
    if (item.quantity < 1) {
        throw new RangeError(`Quantity must be at least 1 for ${item.isbn}`);
    }
}

checkQuantity({ isbn: "9780141439600", quantity: 0 });
