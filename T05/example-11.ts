const priceRange: readonly [number, number] = [300, 450];

// Error: Property 'push' does not exist on type 'readonly [number, number]'.
priceRange.push(500);
