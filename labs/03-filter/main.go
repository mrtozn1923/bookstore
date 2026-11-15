// Lab 03: an in-place filter that quietly overwrites its input.
package main

import "fmt"

type Book struct {
	Title string
	Price int64
}

func cheaperInPlace(books []Book, limit int64) []Book {
	result := books[:0] // reuses the input's backing array
	for _, b := range books {
		if b.Price <= limit {
			result = append(result, b)
		}
	}
	return result
}

func main() {
	books := []Book{{"Dune", 34990}, {"The Hobbit", 28950}, {"Middlemarch", 31000}, {"Emma", 19900}}
	cheap := cheaperInPlace(books, 30000)
	fmt.Println(cheap)
	fmt.Println(books)
}
