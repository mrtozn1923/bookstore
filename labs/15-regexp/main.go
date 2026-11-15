// Lab 15: regexp is RE2: linear time, no lookaround, no backreferences.
package main

import (
	"fmt"
	"regexp"
)

// isbnLike only checks the shape; NormalizeISBN also checks the digit.
var isbnLike = regexp.MustCompile(`^97[89]\d{10}$`)

func main() {
	fmt.Println(isbnLike.MatchString("9780441172719"), isbnLike.MatchString("9780441172718"))

	price := regexp.MustCompile(`(\d+),(\d{2}) TL`)
	fmt.Println(price.FindStringSubmatch("Dune 349,90 TL"))

	//lint:ignore SA1000 the lab shows the error on purpose
	_, err := regexp.Compile(`(?<=ORD-)\d+`) // lookbehind, fine in .NET
	fmt.Println(err)
}
