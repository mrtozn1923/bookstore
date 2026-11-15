package orders

import (
	"testing"
	"time"
)

func TestDeliveryEstimate(t *testing.T) {
	ist, err := time.LoadLocation("Europe/Istanbul")
	if err != nil {
		t.Fatal(err)
	}
	rule := DeliveryRule{Loc: ist, CutoffHour: 15, TransitDays: 2}
	tests := []struct {
		name   string
		placed time.Time
		want   string
	}{
		{"friday before cutoff", time.Date(2026, 11, 13, 14, 59, 0, 0, ist), "2026-11-17"},
		{"friday at cutoff", time.Date(2026, 11, 13, 15, 0, 0, 0, ist), "2026-11-18"},
		{"saturday", time.Date(2026, 11, 14, 10, 0, 0, 0, ist), "2026-11-18"},
		{"utc noon is 15:00 in Istanbul", time.Date(2026, 11, 16, 12, 0, 0, 0, time.UTC), "2026-11-19"},
		{"utc 21:30 is already tuesday", time.Date(2026, 11, 16, 21, 30, 0, 0, time.UTC), "2026-11-19"},
	}
	for _, tt := range tests {
		t.Run(tt.name, func(t *testing.T) {
			got := rule.Estimate(tt.placed)
			if s := got.Format(time.DateOnly); s != tt.want {
				t.Errorf("Estimate(%v) = %s, want %s", tt.placed, s, tt.want)
			}
			if got.Location() != ist || got.Hour() != 0 {
				t.Errorf("Estimate(%v) = %v, want midnight in Istanbul", tt.placed, got)
			}
		})
	}
}

func TestDeliveryEstimateNilLocationIsUTC(t *testing.T) {
	got := DeliveryRule{CutoffHour: 15, TransitDays: 1}.Estimate(time.Date(2026, 11, 16, 14, 0, 0, 0, time.UTC))
	if got.Format(time.DateOnly) != "2026-11-17" || got.Location() != time.UTC {
		t.Errorf("got %v", got)
	}
}
