package catalog

import (
	"errors"
	"slices"
	"testing"
)

func titles(list []Book) []string {
	out := make([]string, len(list))
	for i, b := range list {
		out[i] = b.Title
	}
	return out
}

func TestSortBooks(t *testing.T) {
	tests := []struct {
		spec string
		want []string
	}{
		{"price", []string{"Persuasion", "Emma", "Sense and Sensibility", "Foundation", "The Hobbit",
			"Middlemarch", "The Left Hand of Darkness", "Dune"}}, // a tie at 310.00 keeps catalog order
		{"-price,title", []string{"Dune", "Middlemarch", "The Left Hand of Darkness", "The Hobbit",
			"Foundation", "Sense and Sensibility", "Emma", "Persuasion"}},
		{"category, -stock", []string{"Emma", "Sense and Sensibility", "Middlemarch", "Persuasion",
			"The Hobbit", "Dune", "Foundation", "The Left Hand of Darkness"}},
	}
	for _, tt := range tests {
		list := Store{}.All()
		if err := SortBooks(list, tt.spec); err != nil {
			t.Fatalf("SortBooks(%q): %v", tt.spec, err)
		}
		if got := titles(list); !slices.Equal(got, tt.want) {
			t.Errorf("SortBooks(%q) = %q\nwant %q", tt.spec, got, tt.want)
		}
	}
}

func TestSortBooksUnknownField(t *testing.T) {
	err := SortBooks(Store{}.All(), "price,rating")
	if !errors.Is(err, ErrInvalidSort) {
		t.Fatalf("err = %v, want ErrInvalidSort", err)
	}
	want := `invalid sort: unknown field "rating", want one of author, category, price, stock, title`
	if err.Error() != want {
		t.Errorf("err = %q\nwant %q", err, want)
	}
}

func TestCategories(t *testing.T) {
	want := []CategorySummary{
		{Name: "Classics", Titles: 4, Stock: 19, Cheapest: 18500},
		{Name: "Fantasy", Titles: 1, Stock: 0, Cheapest: 28950},
		{Name: "Science Fiction", Titles: 3, Stock: 23, Cheapest: 27500},
	}
	if got := (Store{}).Categories(); !slices.Equal(got, want) {
		t.Errorf("Categories() = %+v\nwant %+v", got, want)
	}
}
