// Package alloc measures one small heap allocation, to compare Go 1.27's
// size-specialized allocator with GOEXPERIMENT=nosizespecializedmalloc.
package alloc

import "testing"

type orderLine struct {
	ISBN     string
	Quantity int
	Price    int64
}

var sink *orderLine

func BenchmarkSmallAlloc(b *testing.B) {
	for b.Loop() {
		sink = &orderLine{ISBN: "9780441172719", Quantity: 1, Price: 34990} // 32 bytes on the heap
	}
}
