// Lab 17: byte order versus Turkish alphabetical order.
package main

import (
	"fmt"
	"slices"

	"golang.org/x/text/collate"
	"golang.org/x/text/language"
)

func main() {
	authors := []string{"Zülfü Livaneli", "Şükrü Erbaş", "Sabahattin Ali", "Çetin Altan", "Cemal Süreya", "Ömer Seyfettin", "Orhan Pamuk", "İlhan Berk"}

	byteOrder := slices.Clone(authors)
	slices.Sort(byteOrder) // compares UTF-8 bytes
	fmt.Println(byteOrder)

	tr := collate.New(language.Turkish)
	trOrder := slices.Clone(authors)
	tr.SortStrings(trOrder)
	fmt.Println(trOrder)

	fmt.Println(tr.CompareString("ılık", "ilik"), tr.CompareString("Çay", "Cay"))
}
