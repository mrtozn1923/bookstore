// Lab 18: writes a large synthetic API log for the report lab.
package main

import (
	"bufio"
	"flag"
	"fmt"
	"math/rand/v2"
	"os"
	"time"
)

func main() {
	out := flag.String("out", "orders.log", "file to write")
	mb := flag.Int("mb", 1024, "approximate size in MB")
	flag.Parse()
	if err := write(*out, int64(*mb)<<20); err != nil {
		fmt.Fprintln(os.Stderr, "genlog:", err)
		os.Exit(1)
	}
}

func write(path string, size int64) (err error) {
	f, err := os.Create(path)
	if err != nil {
		return err
	}
	defer func() {
		if cerr := f.Close(); err == nil {
			err = cerr // a failed Close can mean the data never reached the disk
		}
	}()
	w := bufio.NewWriterSize(f, 1<<20)
	r := rand.New(rand.NewPCG(18, 2026)) // fixed seed: the same file every time
	t := time.Date(2026, 11, 1, 0, 0, 0, 0, time.UTC)
	var written int64
	for id := 1001; written < size; id++ {
		t = t.Add(time.Duration(r.IntN(1600)) * time.Millisecond)
		items := 1 + r.IntN(3)
		total := items * (18500 + r.IntN(16490))
		n, err := fmt.Fprintf(w, `{"time":"%s","level":"INFO","msg":"order placed","order_id":"ORD-%d","items":%d,"total":"%d.%02d","request_id":"R%d"}`+"\n",
			t.Format(time.RFC3339Nano), id, items, total/100, total%100, id)
		if err != nil {
			return err
		}
		written += int64(n)
		for range 3 { // three other requests per order
			n, err := fmt.Fprintf(w, `{"time":"%s","level":"INFO","msg":"request","method":"GET","path":"/api/books/9780441172719","status":200,"bytes":119,"duration_ms":0.041,"request_id":"R%d"}`+"\n",
				t.Format(time.RFC3339Nano), id)
			if err != nil {
				return err
			}
			written += int64(n)
		}
	}
	return w.Flush() // without Flush the last buffer never reaches the file
}
