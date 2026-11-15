// re2check tries a Search Console regex against sample queries.
// Go's regexp package uses the RE2 syntax, like Search Console.
package main

import (
	"bufio"
	"fmt"
	"os"
	"regexp"
)

func main() {
	if len(os.Args) != 3 {
		fmt.Fprintln(os.Stderr, "usage: re2check <pattern> <queries.txt>")
		os.Exit(2)
	}
	// Search Console matches case-insensitively by default
	re, err := regexp.Compile("(?i)" + os.Args[1])
	if err != nil {
		fmt.Println("invalid RE2 pattern:", err)
		os.Exit(1)
	}
	f, err := os.Open(os.Args[2])
	if err != nil {
		panic(err)
	}
	defer f.Close()
	s := bufio.NewScanner(f)
	for s.Scan() {
		mark := " "
		if re.MatchString(s.Text()) {
			mark = "x"
		}
		fmt.Printf("[%s] %s\n", mark, s.Text())
	}
}
