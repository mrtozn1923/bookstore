// Lab 18: composing readers and writers to store an uploaded cover image.
package main

import (
	"crypto/sha256"
	"errors"
	"fmt"
	"io"
	"os"
	"path/filepath"
	"strings"
)

const maxCover = 1 << 20 // 1 MB

var errTooLarge = errors.New("cover is larger than 1 MB")

// saveCover copies body into dir/name, at most maxCover bytes, and returns
// the size and the SHA-256 of what it wrote, in a single pass.
func saveCover(dir, name string, body io.Reader) (n int64, sum string, err error) {
	f, err := os.Create(filepath.Join(dir, name))
	if err != nil {
		return 0, "", err
	}
	defer func() { err = errors.Join(err, f.Close()) }()

	h := sha256.New()
	limited := io.LimitReader(body, maxCover+1) // one byte more, to notice a body that is too big
	n, err = io.Copy(io.MultiWriter(f, h), limited)
	if err != nil {
		return n, "", err
	}
	if n > maxCover {
		return n, "", errTooLarge
	}
	return n, fmt.Sprintf("%x", h.Sum(nil))[:16], nil
}

func main() {
	dir, err := os.MkdirTemp("", "covers")
	if err != nil {
		panic(err)
	}
	defer os.RemoveAll(dir)

	small := strings.NewReader(strings.Repeat("jpeg", 1000)) // 4,000 bytes
	fmt.Println(saveCover(dir, "9780441172719.jpg", small))

	big := io.MultiReader(strings.NewReader("jpeg"), strings.NewReader(strings.Repeat("x", maxCover)))
	fmt.Println(saveCover(dir, "9780553293357.jpg", big))

	var seen strings.Builder
	tee := io.TeeReader(strings.NewReader("Dune\nEmma\n"), &seen) // everything read is copied to seen
	first := make([]byte, 5)
	k, _ := io.ReadFull(tee, first)
	fmt.Printf("%d %q %q\n", k, first, seen.String())
}
