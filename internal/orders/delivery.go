package orders

import "time"

// DeliveryRule estimates when an order arrives. The warehouse works in one
// time zone: an order placed before the cutoff ships the same business day,
// later orders ship the next business day.
type DeliveryRule struct {
	Loc         *time.Location // nil means UTC
	CutoffHour  int            // local hour, e.g. 15 for 15:00
	TransitDays int            // business days from shipping to delivery
}

// Estimate returns the delivery date as midnight in r.Loc.
func (r DeliveryRule) Estimate(placed time.Time) time.Time {
	loc := r.Loc
	if loc == nil {
		loc = time.UTC
	}
	local := placed.In(loc) // the same instant on the warehouse clock
	day := time.Date(local.Year(), local.Month(), local.Day(), 0, 0, 0, 0, loc)
	if local.Hour() >= r.CutoffHour || !isBusinessDay(day) {
		day = nextBusinessDay(day)
	}
	for range r.TransitDays {
		day = nextBusinessDay(day)
	}
	return day
}

// nextBusinessDay adds calendar days with AddDate, not 24*time.Hour, so a
// daylight saving change cannot move the result off midnight.
func nextBusinessDay(day time.Time) time.Time {
	day = day.AddDate(0, 0, 1)
	for !isBusinessDay(day) {
		day = day.AddDate(0, 0, 1)
	}
	return day
}

func isBusinessDay(day time.Time) bool {
	wd := day.Weekday()
	return wd != time.Saturday && wd != time.Sunday
}
