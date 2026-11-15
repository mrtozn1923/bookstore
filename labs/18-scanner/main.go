// Lab 18: bufio.Scanner stops at a line longer than its buffer.
package main

import (
	"bufio"
	"fmt"
	"strings"
)

func main() {
	log := "line 1\n" + strings.Repeat("x", 70_000) + "\nline 3\n" // a 70 KB line in the middle
	sc := bufio.NewScanner(strings.NewReader(log))
	n := 0
	for sc.Scan() {
		n++
	}
	fmt.Println("lines read:", n) // looks like a short file
	fmt.Println("sc.Err():", sc.Err())

	sc = bufio.NewScanner(strings.NewReader(log))
	sc.Buffer(make([]byte, 0, 64<<10), 1<<20) // allow lines up to 1 MB
	n = 0
	for sc.Scan() {
		n++
	}
	fmt.Println("lines read:", n, "err:", sc.Err())
}
