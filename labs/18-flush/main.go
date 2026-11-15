// Lab 18: a csv.Writer only writes what you flush.
package main

import (
	"encoding/csv"
	"fmt"
	"os"
	"path/filepath"
)

func size(path string) int64 {
	fi, err := os.Stat(path)
	if err != nil {
		return -1
	}
	return fi.Size()
}

func main() {
	dir, err := os.MkdirTemp("", "flush")
	if err != nil {
		panic(err)
	}
	defer os.RemoveAll(dir)

	for _, flush := range []bool{false, true} {
		path := filepath.Join(dir, fmt.Sprintf("daily-%v.csv", flush))
		f, err := os.Create(path)
		if err != nil {
			panic(err)
		}
		cw := csv.NewWriter(f) // buffers in a bufio.Writer of its own
		_ = cw.Write([]string{"date", "orders"})
		_ = cw.Write([]string{"2026-11-14", "108330"})
		if flush {
			cw.Flush()
			if err := cw.Error(); err != nil {
				panic(err)
			}
		}
		if err := f.Close(); err != nil { // closing the file does not flush the csv.Writer
			panic(err)
		}
		fmt.Printf("flush=%v: %d bytes on disk\n", flush, size(path))
	}
}
