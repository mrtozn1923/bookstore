// Lab 15: bytes, runes and what len really counts.
package main

import (
	"fmt"
	"unicode/utf8"
)

// firstN returns at most n characters of s without cutting a rune in half.
func firstN(s string, n int) string {
	i := 0
	for pos := range s { // pos is the byte index where each rune starts
		if i == n {
			return s[:pos]
		}
		i++
	}
	return s
}

func main() {
	title := "Çalıkuşu"
	fmt.Println(len(title), utf8.RuneCountInString(title))
	fmt.Printf("%q %q\n", title[:5], firstN(title, 5))

	for pos, r := range "Dün" {
		fmt.Printf("%d:%c(%U) ", pos, r, r)
	}
	fmt.Println()

	b := []byte("Dune")
	b[0] = 'J' // a []byte can change; the string it came from cannot
	fmt.Println(string(b))

	broken := "Emma\xff" // invalid UTF-8 from a badly encoded file
	fmt.Println(utf8.ValidString(broken), []rune(broken)[4] == utf8.RuneError)
}
