// Package paging splits lists into pages for API responses.
package paging

// Page is one page of a longer list.
type Page[T any] struct {
	Items      []T `json:"items"`
	Page       int `json:"page"`
	PageSize   int `json:"pageSize"`
	TotalItems int `json:"totalItems"`
	TotalPages int `json:"totalPages"`
}

// Of returns page number page (1-based) of items, size items per page.
// The caller validates page >= 1 and size >= 1.
func Of[T any](items []T, page, size int) Page[T] {
	total := len(items)
	start := min((page-1)*size, total)
	end := min(start+size, total)
	return Page[T]{
		Items:      items[start:end:end], // cap = len: an append cannot overwrite the next page
		Page:       page,
		PageSize:   size,
		TotalItems: total,
		TotalPages: (total + size - 1) / size,
	}
}
