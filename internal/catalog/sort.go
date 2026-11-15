package catalog

import (
	"cmp"
	"errors"
	"fmt"
	"maps"
	"slices"
	"strings"
)

// ErrInvalidSort is returned for a sort field the catalog does not know.
var ErrInvalidSort = errors.New("invalid sort")

// bookOrders compares two books by one field, in ascending order.
var bookOrders = map[string]func(a, b Book) int{
	"title":    func(a, b Book) int { return cmp.Compare(a.Title, b.Title) },
	"author":   func(a, b Book) int { return cmp.Compare(a.Author, b.Author) },
	"category": func(a, b Book) int { return cmp.Compare(a.Category, b.Category) },
	"price":    func(a, b Book) int { return cmp.Compare(a.Price, b.Price) },
	"stock":    func(a, b Book) int { return cmp.Compare(a.Stock, b.Stock) },
}

// SortBooks sorts list in place by a spec such as "category,-price": fields
// separated by commas, a leading "-" for descending. The sort is stable, so
// books that compare equal keep their catalog order.
func SortBooks(list []Book, spec string) error {
	var keys []func(a, b Book) int
	for field := range strings.SplitSeq(spec, ",") {
		name, desc := strings.CutPrefix(strings.TrimSpace(field), "-")
		asc, ok := bookOrders[name]
		if !ok {
			return fmt.Errorf("%w: unknown field %q, want one of %s", ErrInvalidSort, name,
				strings.Join(slices.Sorted(maps.Keys(bookOrders)), ", "))
		}
		if desc {
			keys = append(keys, func(a, b Book) int { return asc(b, a) })
		} else {
			keys = append(keys, asc)
		}
	}
	slices.SortStableFunc(list, func(a, b Book) int {
		for _, key := range keys {
			if c := key(a, b); c != 0 {
				return c
			}
		}
		return 0
	})
	return nil
}

// CategorySummary is one line of GET /api/categories.
type CategorySummary struct {
	Name     string `json:"name"`
	Titles   int    `json:"titles"`
	Stock    int    `json:"stock"`
	Cheapest Money  `json:"cheapest"`
}

// Categories groups the catalog by category, sorted by name.
func (Store) Categories() []CategorySummary {
	byName := make(map[string]*CategorySummary)
	for _, b := range books {
		s, ok := byName[b.Category]
		if !ok {
			s = &CategorySummary{Name: b.Category, Cheapest: b.Price}
			byName[b.Category] = s
		}
		s.Titles++
		s.Stock += b.Stock
		s.Cheapest = min(s.Cheapest, b.Price)
	}
	out := make([]CategorySummary, 0, len(byName))
	for _, name := range slices.Sorted(maps.Keys(byName)) { // map order is random
		out = append(out, *byName[name])
	}
	return out
}
