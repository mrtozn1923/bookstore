// Lab 16: one instant, several wall clocks.
package main

import (
	"fmt"
	"time"
)

func must(loc *time.Location, err error) *time.Location {
	if err != nil {
		panic(err)
	}
	return loc
}

func main() {
	ist := must(time.LoadLocation("Europe/Istanbul"))
	berlin := must(time.LoadLocation("Europe/Berlin"))

	placed := time.Date(2026, 11, 14, 21, 30, 0, 0, ist)
	fmt.Println(placed, "|", placed.UTC(), "|", placed.In(berlin))
	fmt.Println(placed.Equal(placed.UTC()), placed == placed.UTC())

	// Parse without an offset means UTC; ParseInLocation means local wall clock.
	a, _ := time.Parse(time.DateTime, "2026-11-14 21:30:00")
	b, _ := time.ParseInLocation(time.DateTime, "2026-11-14 21:30:00", ist)
	fmt.Println(a, "|", b, "|", a.Sub(b))

	// Post 11: datetime2 keeps the wall clock and drops the offset.
	stored := time.Date(placed.Year(), placed.Month(), placed.Day(), placed.Hour(), placed.Minute(), 0, 0, time.UTC)
	fmt.Println("read back:", stored, "shift:", stored.Sub(placed))

	// Truncate works on absolute time, so a "day" starts at 00:00 UTC.
	fmt.Println(placed.Truncate(24 * time.Hour))

	// Berlin leaves daylight saving time on 2026-10-25.
	noon := time.Date(2026, 10, 24, 12, 0, 0, 0, berlin)
	fmt.Println(noon.Add(24*time.Hour), "|", noon.AddDate(0, 0, 1))
}
