// Lab 18: the daily sales report over a log file, plain or gzipped.
package main

import (
	"compress/gzip"
	"errors"
	"fmt"
	"io"
	"os"
	"runtime"
	"strings"
	"time"

	"bookstore/internal/report"
)

func main() {
	if len(os.Args) != 3 {
		fmt.Fprintln(os.Stderr, "usage: report <orders.log[.gz]> <out.csv>")
		os.Exit(2)
	}
	start := time.Now()
	if err := run(os.Args[1], os.Args[2]); err != nil {
		fmt.Fprintln(os.Stderr, "report:", err)
		os.Exit(1)
	}
	var m runtime.MemStats
	runtime.ReadMemStats(&m)
	fmt.Printf("done in %v, memory from the OS: %d MB\n", time.Since(start).Round(100*time.Millisecond), m.Sys>>20)
}

func run(in, out string) (err error) {
	f, err := os.Open(in)
	if err != nil {
		return err
	}
	defer f.Close() // read-only: a Close error loses nothing

	var r io.Reader = f
	if strings.HasSuffix(in, ".gz") {
		gz, err := gzip.NewReader(f) // a Reader that wraps a Reader
		if err != nil {
			return err
		}
		defer gz.Close() // does not close f
		r = gz
	}

	w, err := os.Create(out)
	if err != nil {
		return err
	}
	defer func() { err = errors.Join(err, w.Close()) }() // the write is not done until Close succeeds

	ist, err := time.LoadLocation("Europe/Istanbul")
	if err != nil {
		return err
	}
	return report.DailySales(r, w, ist)
}
