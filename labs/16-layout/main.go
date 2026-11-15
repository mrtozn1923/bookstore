// Lab 16: layouts are written with the reference time, not with yyyy-MM-dd.
package main

import (
	"fmt"
	"time"
)

// swapped is a variable, not a constant, so go vet does not see it.
var swapped = "2006-02-01 03:04"

func main() {
	ist, err := time.LoadLocation("Europe/Istanbul")
	if err != nil {
		panic(err)
	}
	t := time.Date(2026, 11, 14, 21, 5, 7, 123456789, ist)

	fmt.Println(t.Format("yyyy-MM-dd HH:mm")) // the C# habit: no error, no date
	fmt.Println(t.Format(swapped))            // day and month swapped, 12-hour clock
	fmt.Println(t.Format("2006-01-02 15:04"))
	fmt.Println(t.Format("02.01.2006 Monday"))
	fmt.Println(t.Format(time.RFC3339), "|", t.Format(time.RFC3339Nano))
	fmt.Println(t.Format(time.DateOnly), "|", t.Format(time.Kitchen))

	for _, s := range []string{"2026-11-14", "14.11.2026", "2026-11-31"} {
		d, err := time.Parse(time.DateOnly, s)
		fmt.Println(d, err)
	}
}
