// Lab 15: strconv errors and a supplier price list in Turkish number format.
package main

import (
	"errors"
	"fmt"
	"strconv"
	"strings"

	"bookstore/internal/catalog"
)

// priceList is what a supplier exports from a Turkish Excel sheet.
const priceList = `isbn;title;price
978-0-441-17271-9; Dune ;349,90 TL
9780547928227;The Hobbit;1.249,50 TL
978-0-14-143954-9;Middlemarch;310
9780141439587;Emma;abc`

// parseTRPrice reads "1.249,50 TL": dot for thousands, comma for decimals.
func parseTRPrice(s string) (catalog.Money, error) {
	s = strings.TrimSpace(strings.TrimSuffix(strings.TrimSpace(s), "TL"))
	s = strings.ReplaceAll(s, ".", "")
	s = strings.Replace(s, ",", ".", 1)
	return catalog.ParseMoney(s)
}

func main() {
	for _, in := range []string{"42", "12a", "99999999999999999999"} {
		n, err := strconv.Atoi(in)
		fmt.Println(n, err, errors.Is(err, strconv.ErrSyntax), errors.Is(err, strconv.ErrRange))
	}
	f, err := strconv.ParseFloat("349,90", 64)
	fmt.Println(f, err)
	fmt.Println(strconv.Quote("Dune\t\"Arrakis\""), strconv.FormatInt(34990, 10))

	lines := strings.Split(priceList, "\n")
	for i, line := range lines[1:] { // skip the header
		fields := strings.Split(line, ";")
		if len(fields) != 3 {
			fmt.Printf("line %d: want 3 fields, got %d\n", i+2, len(fields))
			continue
		}
		isbn, err := catalog.NormalizeISBN(fields[0])
		if err != nil {
			fmt.Printf("line %d: %v\n", i+2, err)
			continue
		}
		price, err := parseTRPrice(fields[2])
		if err != nil {
			fmt.Printf("line %d: %v\n", i+2, err)
			continue
		}
		fmt.Printf("line %d: %s %-15q %v\n", i+2, isbn, strings.TrimSpace(fields[1]), price)
	}
}
