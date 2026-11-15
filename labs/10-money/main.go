// Lab 10: three ways to carry a price in JSON.
package main

import (
	"encoding/json"
	"fmt"

	"bookstore/internal/catalog"
)

// Decimal shows a price as a string such as "349.90" in JSON.
type Decimal catalog.Money

// MarshalText makes encoding/json write the value as a JSON string.
func (d Decimal) MarshalText() ([]byte, error) {
	return fmt.Appendf(nil, "%d.%02d", d/100, d%100), nil
}

func main() {
	// float64: 3 x Dune plus 1 x Emma, then the 10% institutional discount.
	dune, emma, rate := 349.90, 199.00, 0.9 // variables: no exact constant arithmetic
	subtotal := dune*3 + emma
	f := subtotal * rate
	fmt.Println("float64:", subtotal, f)

	// int kuruş: the same calculation; the rounding rule is written down.
	sub := catalog.Money(34990)*3 + 19900
	k := sub * 90 / 100 // integer division: rounds down
	fmt.Println("kurus:  ", int64(sub), int64(k), k)

	out, _ := json.Marshal(map[string]any{"float": f, "kurus": k, "decimal": Decimal(k)})
	fmt.Println(string(out))
}
