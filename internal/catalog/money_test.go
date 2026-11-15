package catalog

import (
	"errors"
	"math"
	"testing"
)

func TestParseMoney(t *testing.T) {
	tests := []struct {
		in      string
		want    Money
		wantErr bool
	}{
		{"349.90", 34990, false},
		{"349.9", 34990, false},
		{"349", 34900, false},
		{"0.05", 5, false},
		{"-1.50", -150, false},
		{"349,90", 0, true}, // Turkish decimal comma
		{"349.", 0, true},
		{"3.499", 0, true},
		{"", 0, true},
		{"1e3", 0, true},
	}
	for _, tt := range tests {
		got, err := ParseMoney(tt.in)
		if (err != nil) != tt.wantErr || got != tt.want {
			t.Errorf("ParseMoney(%q) = %d, %v; want %d, error %v", tt.in, got, err, tt.want, tt.wantErr)
		}
		if err != nil && !errors.Is(err, ErrInvalidMoney) {
			t.Errorf("ParseMoney(%q) error %v does not wrap ErrInvalidMoney", tt.in, err)
		}
	}
}

func TestMoneyStringNegative(t *testing.T) {
	if got := Money(-150).String(); got != "-1.50 TRY" {
		t.Errorf("Money(-150).String() = %q, want %q", got, "-1.50 TRY")
	}
}

// FuzzMoneyRoundTrip checks that every amount survives Decimal and ParseMoney,
// which is what happens on every trip to the database and back.
func FuzzMoneyRoundTrip(f *testing.F) {
	for _, seed := range []int64{0, 5, 34990, 100, math.MinInt64} {
		f.Add(seed)
	}
	f.Fuzz(func(t *testing.T, n int64) {
		m := Money(n)
		back, err := ParseMoney(m.Decimal())
		if err != nil || back != m {
			t.Fatalf("Money(%d).Decimal() = %q, ParseMoney gives %d, %v", n, m.Decimal(), back, err)
		}
	})
}
