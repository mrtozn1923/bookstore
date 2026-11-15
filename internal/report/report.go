// Package report turns the API's JSON log into a daily sales summary.
package report

import (
	"bufio"
	"bytes"
	"encoding/csv"
	"encoding/json"
	"fmt"
	"io"
	"maps"
	"slices"
	"strconv"
	"time"

	"bookstore/internal/catalog"
)

// MaxLineBytes is the longest log line DailySales accepts. bufio.Scanner
// stops at 64 KB by default.
const MaxLineBytes = 1 << 20

// orderPlaced is the part of an "order placed" log line we need.
type orderPlaced struct {
	Time  time.Time `json:"time"`
	Items int       `json:"items"`
	Total string    `json:"total"`
}

type day struct {
	orders, items int
	revenue       catalog.Money
}

// marker lets us skip the other log lines without decoding them.
var marker = []byte(`"msg":"order placed"`)

// DailySales reads JSON log lines from r and writes one CSV row per day to
// w. Days are calendar days in loc. It reads one line at a time, so memory
// use does not grow with the size of the log.
func DailySales(r io.Reader, w io.Writer, loc *time.Location) error {
	days := make(map[string]*day)
	sc := bufio.NewScanner(r)
	sc.Buffer(make([]byte, 0, 64<<10), MaxLineBytes)
	line := 0
	for sc.Scan() {
		line++
		if !bytes.Contains(sc.Bytes(), marker) {
			continue
		}
		var e orderPlaced
		if err := json.Unmarshal(sc.Bytes(), &e); err != nil {
			return fmt.Errorf("line %d: %w", line, err)
		}
		total, err := catalog.ParseMoney(e.Total)
		if err != nil {
			return fmt.Errorf("line %d: %w", line, err)
		}
		key := e.Time.In(loc).Format(time.DateOnly)
		d := days[key]
		if d == nil {
			d = &day{}
			days[key] = d
		}
		d.orders++
		d.items += e.Items
		d.revenue += total
	}
	if err := sc.Err(); err != nil { // without this, a long line ends the loop silently
		return fmt.Errorf("after line %d: %w", line, err)
	}

	cw := csv.NewWriter(w)
	if err := cw.Write([]string{"date", "orders", "items", "revenue"}); err != nil {
		return err
	}
	for _, key := range slices.Sorted(maps.Keys(days)) {
		d := days[key]
		row := []string{key, strconv.Itoa(d.orders), strconv.Itoa(d.items), d.revenue.Decimal()}
		if err := cw.Write(row); err != nil {
			return err
		}
	}
	cw.Flush() // csv.Writer buffers too
	return cw.Error()
}
