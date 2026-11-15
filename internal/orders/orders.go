// Package orders models customer orders.
package orders

import (
	"fmt"
	"time"

	"bookstore/internal/catalog"
)

// Status is where an order is in its life cycle.
type Status int

const (
	StatusPending Status = iota
	StatusPaid
	StatusShipped
	StatusCancelled
)

func (s Status) String() string {
	switch s {
	case StatusPending:
		return "Pending"
	case StatusPaid:
		return "Paid"
	case StatusShipped:
		return "Shipped"
	case StatusCancelled:
		return "Cancelled"
	default:
		return fmt.Sprintf("Status(%d)", int(s))
	}
}

// Item is one line of an order.
type Item struct {
	ISBN      string
	Title     string
	UnitPrice catalog.Money
	Quantity  int
}

// Total is the price of the line.
func (i Item) Total() catalog.Money {
	return i.UnitPrice * catalog.Money(i.Quantity)
}

// Timestamps is embedded into types that track when they changed.
type Timestamps struct {
	CreatedAt time.Time
	UpdatedAt time.Time
}

// Touch records a change.
func (t *Timestamps) Touch(now time.Time) {
	t.UpdatedAt = now
}

// Order is a customer's order.
type Order struct {
	Timestamps
	ID         string
	CustomerID string
	Items      []Item
	Status     Status
	PaidAt     *time.Time // nil until the order is paid
}

// NewOrder returns a pending order with no items.
func NewOrder(id, customerID string, now time.Time) *Order {
	return &Order{
		Timestamps: Timestamps{CreatedAt: now, UpdatedAt: now},
		ID:         id,
		CustomerID: customerID,
		Status:     StatusPending,
	}
}

// AddItem appends a line to the order.
func (o *Order) AddItem(item Item, now time.Time) {
	o.Items = append(o.Items, item)
	o.Touch(now)
}

// MarkPaid moves a pending order to Paid.
func (o *Order) MarkPaid(now time.Time) error {
	if o.Status != StatusPending {
		return fmt.Errorf("pay order %s in status %v: %w", o.ID, o.Status, ErrInvalidTransition)
	}
	o.Status = StatusPaid
	o.PaidAt = &now
	o.Touch(now)
	return nil
}

// Total is the sum of all lines.
func (o *Order) Total() catalog.Money {
	var sum catalog.Money
	for _, it := range o.Items {
		sum += it.Total()
	}
	return sum
}

// String implements fmt.Stringer.
func (o *Order) String() string {
	return fmt.Sprintf("order %s (%v): %d items, %v", o.ID, o.Status, len(o.Items), o.Total())
}
