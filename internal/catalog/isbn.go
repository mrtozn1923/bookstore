package catalog

import (
	"errors"
	"fmt"
	"strings"
)

// ErrInvalidISBN is returned for text that is not a valid ISBN-13.
var ErrInvalidISBN = errors.New("invalid ISBN-13")

// NormalizeISBN turns "ISBN 978-0-441-17271-9" into "9780441172719" and
// checks the check digit. Hyphens and spaces are dropped; nothing else is.
func NormalizeISBN(s string) (string, error) {
	s = strings.TrimSpace(s)
	if len(s) >= 4 && strings.EqualFold(s[:4], "isbn") {
		s = strings.TrimLeft(s[4:], " :-")
		s = strings.TrimLeft(strings.TrimPrefix(s, "13"), " :") // "ISBN-13: ..."
	}
	isbn := strings.Map(func(r rune) rune {
		if r == '-' || r == ' ' {
			return -1 // drop the rune
		}
		return r
	}, s)
	if len(isbn) != 13 || !digits(isbn) {
		return "", fmt.Errorf("%q: %w", s, ErrInvalidISBN)
	}
	if !strings.HasPrefix(isbn, "978") && !strings.HasPrefix(isbn, "979") {
		return "", fmt.Errorf("%q: prefix must be 978 or 979: %w", s, ErrInvalidISBN)
	}
	if want := checkDigit(isbn[:12]); isbn[12] != want {
		return "", fmt.Errorf("%q: check digit is %c, want %c: %w", s, isbn[12], want, ErrInvalidISBN)
	}
	return isbn, nil
}

// checkDigit computes the 13th digit: weights 1 and 3 alternate.
func checkDigit(first12 string) byte {
	sum := 0
	for i := range len(first12) {
		d := int(first12[i] - '0') // a byte, not a rune: digits are ASCII
		if i%2 == 1 {
			d *= 3
		}
		sum += d
	}
	return byte('0' + (10-sum%10)%10)
}
