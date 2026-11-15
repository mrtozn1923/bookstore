package catalog

import (
	"database/sql/driver"
	"errors"
	"fmt"
	"strconv"
	"strings"
)

// ErrInvalidMoney is returned for text that is not a decimal amount.
var ErrInvalidMoney = errors.New("invalid money amount")

// Decimal formats the amount without a currency: Money(34990) is "349.90".
func (m Money) Decimal() string {
	return fmt.Sprintf("%d.%02d", m/100, m%100)
}

// ParseMoney reads a decimal amount such as "349.90", "349.9" or "349"
// without going through float64.
func ParseMoney(s string) (Money, error) {
	whole, frac, hasDot := strings.Cut(s, ".")
	neg := strings.HasPrefix(whole, "-")
	whole = strings.TrimPrefix(whole, "-")
	if whole == "" || len(frac) > 2 || (hasDot && frac == "") || !digits(whole) || !digits(frac) {
		return 0, fmt.Errorf("%q: %w", s, ErrInvalidMoney)
	}
	n, err := strconv.ParseInt(whole+(frac + "00")[:2], 10, 64)
	if err != nil {
		return 0, fmt.Errorf("%q: %w", s, ErrInvalidMoney)
	}
	if neg {
		n = -n
	}
	return Money(n), nil
}

func digits(s string) bool {
	for _, c := range s {
		if c < '0' || c > '9' {
			return false
		}
	}
	return true
}

// Scan implements sql.Scanner. SQL Server drivers return decimal columns as
// text ("349.90"), which database/sql cannot convert to an integer type.
func (m *Money) Scan(src any) error {
	switch v := src.(type) {
	case []byte:
		return m.parseInto(string(v))
	case string:
		return m.parseInto(v)
	case int64:
		*m = Money(v * 100)
		return nil
	default:
		return fmt.Errorf("scan %T into Money: %w", src, ErrInvalidMoney)
	}
}

func (m *Money) parseInto(s string) error {
	v, err := ParseMoney(s)
	if err != nil {
		return err
	}
	*m = v
	return nil
}

// Value implements driver.Valuer: the amount goes to the database as
// decimal text, so a decimal(10,2) column receives it without rounding.
func (m Money) Value() (driver.Value, error) {
	return m.Decimal(), nil
}
