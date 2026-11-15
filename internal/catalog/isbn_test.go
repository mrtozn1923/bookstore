package catalog

import (
	"errors"
	"testing"
)

func TestNormalizeISBN(t *testing.T) {
	tests := []struct {
		in, want string
		wantErr  bool
	}{
		{"9780441172719", "9780441172719", false},
		{"978-0-441-17271-9", "9780441172719", false},
		{" ISBN 978 0 441 17271 9 ", "9780441172719", false},
		{"isbn: 978-0-441-17271-9", "9780441172719", false},
		{"ISBN-13: 979-8-88-645174-0", "9798886451740", false},
		{"978-0-441-17271-8", "", true}, // wrong check digit
		{"0-441-17271-7", "", true},     // ISBN-10
		{"977-0-441-17271-5", "", true}, // not a book prefix
		{"978-0-441-17271-X", "", true}, // X exists only in ISBN-10
		{"٩٧٨٠٤٤١١٧٢٧١٩", "", true},     // Arabic-Indic digits are not ASCII digits
		{"", "", true},
	}
	for _, tt := range tests {
		got, err := NormalizeISBN(tt.in)
		if got != tt.want || (err != nil) != tt.wantErr {
			t.Errorf("NormalizeISBN(%q) = %q, %v; want %q, error %v", tt.in, got, err, tt.want, tt.wantErr)
		}
		if err != nil && !errors.Is(err, ErrInvalidISBN) {
			t.Errorf("NormalizeISBN(%q) error %v does not wrap ErrInvalidISBN", tt.in, err)
		}
	}
}

func TestCatalogISBNsAreValid(t *testing.T) {
	for b := range (Store{}).Books() {
		if got, err := NormalizeISBN(b.ISBN); err != nil || got != b.ISBN {
			t.Errorf("catalog ISBN %s: %q, %v", b.ISBN, got, err)
		}
	}
}
