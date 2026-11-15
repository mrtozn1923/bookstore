// Command 14-osroot serves cover images from one folder and shows that
// os.Root (Go 1.24) refuses names that try to leave that folder.
package main

import (
	"fmt"
	"os"
	"path/filepath"
)

func main() {
	dir, err := os.MkdirTemp("", "covers")
	if err != nil {
		panic(err)
	}
	defer os.RemoveAll(dir)
	covers := filepath.Join(dir, "covers")
	if err := os.Mkdir(covers, 0o755); err != nil {
		panic(err)
	}
	os.WriteFile(filepath.Join(covers, "9780441172719.jpg"), []byte("jpeg"), 0o644)
	os.WriteFile(filepath.Join(dir, "secrets.env"), []byte("DB_PASSWORD=example-password-change-me"), 0o600)

	root, err := os.OpenRoot(covers)
	if err != nil {
		panic(err)
	}
	defer root.Close()

	for _, name := range []string{"9780441172719.jpg", "../secrets.env"} {
		// os.Open(filepath.Join(covers, name)) would happily open ../secrets.env.
		pf, plainErr := os.Open(filepath.Join(covers, name))
		if plainErr == nil {
			pf.Close()
		}
		f, err := root.Open(name)
		if err == nil {
			f.Close()
		}
		fmt.Printf("%-20s os.Open: %v | root.Open: %v\n", name, errText(plainErr), errText(err))
	}
}

func errText(err error) string {
	if err == nil {
		return "ok"
	}
	return err.Error()
}
