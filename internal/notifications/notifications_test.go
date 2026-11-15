package notifications

import (
	"strings"
	"testing"
	"time"

	"bookstore/internal/orders"
)

func TestOrderConfirmation(t *testing.T) {
	ist, err := time.LoadLocation("Europe/Istanbul")
	if err != nil {
		t.Fatal(err)
	}
	placed := time.Date(2026, 11, 13, 11, 59, 0, 0, time.UTC) // 14:59 in Istanbul
	o := orders.NewOrder("ORD-1001", "ada", placed)
	o.AddItem(orders.Item{ISBN: "9780441172719", Title: "Dune", UnitPrice: 34990, Quantity: 2}, placed)

	var sb strings.Builder // a strings.Builder is an io.Writer too
	if err := OrderConfirmation(&sb, o, orders.DeliveryRule{Loc: ist, CutoffHour: 15, TransitDays: 2}); err != nil {
		t.Fatal(err)
	}
	for _, want := range []string{
		"Subject: Your Bookstore order ORD-1001",
		"We received it on 13.11.2026 14:59.",
		"2 x Dune (9780441172719)  699.80 TRY",
		"Estimated delivery: 17.11.2026",
	} {
		if !strings.Contains(sb.String(), want) {
			t.Errorf("mail does not contain %q:\n%s", want, sb.String())
		}
	}
}
