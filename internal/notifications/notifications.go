// Package notifications renders the e-mails Bookstore sends.
package notifications

import (
	"embed"
	"fmt"
	"io"
	"text/template"
	"time"

	"bookstore/internal/catalog"
	"bookstore/internal/orders"
)

// templates is compiled into the binary; there is no file to deploy.
//
//go:embed templates/*.txt
var templates embed.FS

var mails = template.Must(template.ParseFS(templates, "templates/*.txt"))

// confirmation is what the template sees.
type confirmation struct {
	ID, CustomerID   string
	Placed, Delivery string
	Items            []line
	Total            catalog.Money
}

type line struct {
	ISBN, Title string
	Quantity    int
	Total       catalog.Money
}

// OrderConfirmation writes the confirmation e-mail for o to w. Times are
// shown on the warehouse clock that rule uses.
func OrderConfirmation(w io.Writer, o *orders.Order, rule orders.DeliveryRule) error {
	loc := rule.Loc
	if loc == nil {
		loc = time.UTC
	}
	c := confirmation{
		ID: o.ID, CustomerID: o.CustomerID,
		Placed:   o.CreatedAt.In(loc).Format("02.01.2006 15:04"),
		Delivery: rule.Estimate(o.CreatedAt).Format("02.01.2006"),
		Total:    o.Total(),
	}
	for _, it := range o.Items {
		c.Items = append(c.Items, line{ISBN: it.ISBN, Title: it.Title, Quantity: it.Quantity, Total: it.Total()})
	}
	if err := mails.ExecuteTemplate(w, "order_confirmation.txt", c); err != nil {
		return fmt.Errorf("render confirmation for %s: %w", o.ID, err)
	}
	return nil
}
