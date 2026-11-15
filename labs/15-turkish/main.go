// Lab 15: case conversion and comparison with Turkish text.
package main

import (
	"fmt"
	"strings"
	"unicode"
)

func main() {
	tr := unicode.TurkishCase
	fmt.Println(strings.ToUpper("istanbul"), strings.ToUpperSpecial(tr, "istanbul"))
	fmt.Println(strings.ToLower("ISPARTA"), strings.ToLowerSpecial(tr, "ISPARTA"))
	fmt.Println(strings.ToLower("İZMİR"), strings.ToLowerSpecial(tr, "İZMİR"))

	fmt.Println(strings.EqualFold("Dune", "DUNE"))
	fmt.Println(strings.EqualFold("istanbul", "İSTANBUL"), strings.EqualFold("ısparta", "ISPARTA"))
}
