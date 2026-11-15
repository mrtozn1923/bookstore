package paging

import (
	"fmt"
	"testing"
)

type book struct {
	ISBN  string
	Title string
	Price int64
}

// bookPage and ofBooks are the hand-written, non-generic version of Page
// and Of, for comparison.
type bookPage struct {
	Items                                  []book
	Page, PageSize, TotalItems, TotalPages int
}

func ofBooks(items []book, page, size int) bookPage {
	total := len(items)
	start := min((page-1)*size, total)
	end := min(start+size, total)
	return bookPage{Items: items[start:end:end], Page: page, PageSize: size,
		TotalItems: total, TotalPages: (total + size - 1) / size}
}

var sink int

func makeBooks(n int) []book {
	bs := make([]book, n)
	for i := range bs {
		bs[i] = book{ISBN: fmt.Sprintf("978%010d", i), Title: "Dune", Price: 34990}
	}
	return bs
}

func BenchmarkOfGeneric(b *testing.B) {
	bs := makeBooks(10_000)
	for b.Loop() { // Go 1.24: setup above is not timed, the loop body is not optimised away
		p := Of(bs, 37, 20)
		sink += len(p.Items)
	}
}

func BenchmarkOfBooks(b *testing.B) {
	bs := makeBooks(10_000)
	for b.Loop() {
		p := ofBooks(bs, 37, 20)
		sink += len(p.Items)
	}
}

func TestOfLastPage(t *testing.T) {
	p := Of(makeBooks(45), 3, 20)
	if len(p.Items) != 5 || cap(p.Items) != 5 || p.TotalPages != 3 {
		t.Fatalf("got %d items, cap %d, %d pages; want 5, 5, 3", len(p.Items), cap(p.Items), p.TotalPages)
	}
}
