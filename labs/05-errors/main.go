// Lab 05: sentinel errors, wrapping, errors.Is/As/AsType and errors.Join.
package main

import (
	"context"
	"errors"
	"fmt"
	"log"
	"os"
	"time"

	"bookstore/internal/catalog"
	"bookstore/internal/orders"
	"bookstore/internal/orders/memstore"
)

func main() {
	ctx := context.Background()
	repo := memstore.New()
	svc := orders.NewService(repo, catalog.Store{})

	// 1) Not enough stock: a wrapped sentinel error.
	_, err := svc.Place(ctx, "ada", []orders.Line{{ISBN: "9780141439549", Quantity: 5}})
	fmt.Println(err)
	fmt.Println(errors.Is(err, orders.ErrOutOfStock), err == orders.ErrOutOfStock)

	// 2) Unknown ISBN: the catalog's sentinel passes through two wraps.
	_, err = svc.Place(ctx, "ada", []orders.Line{{ISBN: "9780000000000", Quantity: 1}})
	fmt.Println(err)
	fmt.Println(errors.Is(err, catalog.ErrBookNotFound))

	// 3) Several validation problems at once: errors.Join.
	_, err = svc.Place(ctx, "", []orders.Line{{ISBN: "9780441172719", Quantity: 0}})
	fmt.Printf("%v\n---\n", err)

	var ve orders.ValidationError
	if errors.As(err, &ve) {
		fmt.Println("errors.As:", ve.Field)
	}
	if v, ok := errors.AsType[orders.ValidationError](err); ok {
		fmt.Println("errors.AsType:", v.Field, "-", v.Message)
	}

	// 4) The happy path, then an invalid status transition.
	o, err := svc.Place(ctx, "ada", []orders.Line{{ISBN: "9780441172719", Quantity: 1}})
	if err != nil {
		fmt.Println("unexpected:", err)
		return
	}
	err = o.MarkPaid(time.Now())
	fmt.Println(o, err)
	err = o.MarkPaid(time.Now())
	fmt.Println(err, errors.Is(err, orders.ErrInvalidTransition))

	// 5) Logging and returning: the same failure appears twice.
	logger := log.New(os.Stdout, "", 0)
	if err := placeAndLog(ctx, svc, logger); err != nil {
		logger.Printf("handler: %v", err)
	}
}

func placeAndLog(ctx context.Context, svc *orders.Service, logger *log.Logger) error {
	_, err := svc.Place(ctx, "grace", []orders.Line{{ISBN: "9780547928227", Quantity: 1}})
	if err != nil {
		logger.Printf("place failed: %v", err) // logs here...
		return err                             // ...and the caller logs it again
	}
	return nil
}
