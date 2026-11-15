package report

import (
	"bufio"
	"bytes"
	"errors"
	"strings"
	"testing"
	"time"
)

const sample = `{"time":"2026-11-14T09:12:03.1+03:00","level":"INFO","msg":"order placed","order_id":"ORD-1001","items":2,"total":"699.80"}
{"time":"2026-11-14T09:12:03.2+03:00","level":"INFO","msg":"request","method":"POST","path":"/api/orders","status":201}
{"time":"2026-11-14T23:59:00+03:00","level":"INFO","msg":"order placed","order_id":"ORD-1002","items":1,"total":"199.00"}
{"time":"2026-11-14T21:30:00Z","level":"INFO","msg":"order placed","order_id":"ORD-1003","items":3,"total":"555.00"}
`

func TestDailySales(t *testing.T) {
	ist, err := time.LoadLocation("Europe/Istanbul")
	if err != nil {
		t.Fatal(err)
	}
	var out bytes.Buffer // any io.Writer will do
	if err := DailySales(strings.NewReader(sample), &out, ist); err != nil {
		t.Fatal(err)
	}
	want := "date,orders,items,revenue\n2026-11-14,2,3,898.80\n2026-11-15,1,3,555.00\n"
	if out.String() != want {
		t.Errorf("got\n%s\nwant\n%s", out.String(), want)
	}
}

func TestDailySalesLongLine(t *testing.T) {
	long := `{"msg":"request","path":"/` + strings.Repeat("a", MaxLineBytes) + `"}` + "\n"
	err := DailySales(strings.NewReader(sample+long+sample), &bytes.Buffer{}, time.UTC)
	if !errors.Is(err, bufio.ErrTooLong) {
		t.Fatalf("err = %v, want bufio.ErrTooLong", err)
	}
}

func TestDailySalesBadTotal(t *testing.T) {
	bad := `{"time":"2026-11-14T09:00:00Z","msg":"order placed","items":1,"total":"699,80"}` + "\n"
	err := DailySales(strings.NewReader(bad), &bytes.Buffer{}, time.UTC)
	if err == nil || !strings.HasPrefix(err.Error(), "line 1: ") {
		t.Fatalf("err = %v, want an error for line 1", err)
	}
}
