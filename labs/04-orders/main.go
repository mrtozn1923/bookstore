// Lab 04: orders, Stringer, type switches and the nil interface trap.
package main

import (
	"context"
	"fmt"
	"time"

	"bookstore/internal/catalog"
	"bookstore/internal/orders"
	"bookstore/internal/orders/memstore"
	"bookstore/internal/orders/sqlstore"
)

func main() {
	ctx := context.Background()
	repo := memstore.New()
	svc := orders.NewService(repo)

	books := catalog.Store{}.All()
	dune, emma := books[0], books[3]
	o, err := svc.Place(ctx, "ada", []orders.Item{
		{ISBN: dune.ISBN, Title: dune.Title, UnitPrice: dune.Price, Quantity: 2},
		{ISBN: emma.ISBN, Title: emma.Title, UnitPrice: emma.Price, Quantity: 1},
	})
	if err != nil {
		fmt.Println("place failed:", err)
		return
	}
	fmt.Println(o)
	fmt.Println(o.MarkPaid(time.Now()), o.MarkPaid(time.Now()), o.Status)

	found, err := repo.ByID(ctx, o.ID)
	fmt.Println(found == o, err)
	_, err = repo.ByID(ctx, "ORD-9999")
	fmt.Println(err)

	for _, v := range []any{o.Total(), o.Status, err, 42, nil} {
		fmt.Println(describe(v))
	}

	var r orders.Repository = repo
	if ms, ok := r.(*memstore.Store); ok {
		fmt.Printf("type assertion: %T\n", ms)
	}

	r = pickRepository(false)
	//lint:ignore SA4023 this lab shows the trap on purpose
	fmt.Printf("r == nil: %v, dynamic type: %T\n", r == nil, r)
}

func describe(v any) string {
	switch x := v.(type) {
	case nil:
		return "nil"
	case catalog.Money:
		return "money: " + x.String()
	case fmt.Stringer:
		return "stringer: " + x.String()
	case error:
		return "error: " + x.Error()
	default:
		return fmt.Sprintf("other: %T", x)
	}
}

// pickRepository has a bug: it returns a typed nil pointer inside an interface.
func pickRepository(useSQL bool) orders.Repository {
	var s *sqlstore.Store
	if useSQL {
		s = sqlstore.New(nil)
	}
	return s
}
