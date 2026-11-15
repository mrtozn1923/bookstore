// Lab 08: 100 customers place an order at the same time.
package main

import (
	"context"
	"fmt"
	"sync"

	"bookstore/internal/catalog"
	"bookstore/internal/orders"
	"bookstore/internal/orders/memstore"
)

func main() {
	ctx := context.Background()
	repo := memstore.New()
	svc := orders.NewService(repo, catalog.Store{})

	const n = 100
	ids := make([]string, n)
	var wg sync.WaitGroup
	for i := range n {
		wg.Go(func() { // Go 1.25: Add(1), go, defer Done() in one call
			o, err := svc.Place(ctx, fmt.Sprintf("customer-%d", i),
				[]orders.Line{{ISBN: "9780441172719", Quantity: 1}})
			if err != nil {
				fmt.Println("place:", err)
				return
			}
			ids[i] = o.ID // each goroutine writes only its own index
		})
	}
	wg.Wait()

	unique := map[string]bool{}
	for _, id := range ids {
		unique[id] = true
	}
	fmt.Printf("orders placed: %d, unique IDs: %d\n", n, len(unique))
}
