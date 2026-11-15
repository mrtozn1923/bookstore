// Lab 15: building one string from 1,000 titles.
package builder

import (
	"strings"
	"testing"
)

var titles = func() []string {
	t := make([]string, 1000)
	for i := range t {
		t[i] = "Middlemarch"
	}
	return t
}()

func BenchmarkPlus(b *testing.B) {
	for b.Loop() {
		s := ""
		for _, t := range titles {
			s += t + "\n" // a new string, and a copy of everything so far, each time
		}
		_ = s
	}
}

func BenchmarkBuilder(b *testing.B) {
	for b.Loop() {
		var sb strings.Builder
		for _, t := range titles {
			sb.WriteString(t)
			sb.WriteByte('\n')
		}
		_ = sb.String()
	}
}

func BenchmarkBuilderGrow(b *testing.B) {
	for b.Loop() {
		var sb strings.Builder
		sb.Grow(len(titles) * (len("Middlemarch") + 1)) // one allocation
		for _, t := range titles {
			sb.WriteString(t)
			sb.WriteByte('\n')
		}
		_ = sb.String()
	}
}

func BenchmarkJoin(b *testing.B) {
	for b.Loop() {
		_ = strings.Join(titles, "\n")
	}
}
