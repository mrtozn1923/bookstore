// Lab 18: the order confirmation rendered from the embedded template.
package main

import (
	"os"
	"time"

	"bookstore/internal/notifications"
	"bookstore/internal/orders"
)

func main() {
	ist, err := time.LoadLocation("Europe/Istanbul")
	if err != nil {
		panic(err)
	}
	placed := time.Date(2026, 11, 13, 11, 59, 0, 0, time.UTC)
	o := orders.NewOrder("ORD-1001", "ada", placed)
	o.AddItem(orders.Item{ISBN: "9780441172719", Title: "Dune", UnitPrice: 34990, Quantity: 2}, placed)
	o.AddItem(orders.Item{ISBN: "9780141439587", Title: "Emma", UnitPrice: 19900, Quantity: 1}, placed)

	rule := orders.DeliveryRule{Loc: ist, CutoffHour: 15, TransitDays: 2}
	if err := notifications.OrderConfirmation(os.Stdout, o, rule); err != nil { // os.Stdout is an io.Writer
		panic(err)
	}
}
