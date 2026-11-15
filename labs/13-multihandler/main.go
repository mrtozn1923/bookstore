// Command 13-multihandler sends every record as JSON to stdout and, with
// slog.NewMultiHandler (Go 1.26), warnings and errors as text to stderr too.
package main

import (
	"log/slog"
	"os"
)

func main() {
	noTime := func(_ []string, a slog.Attr) slog.Attr {
		if a.Key == slog.TimeKey {
			return slog.Attr{} // keep the output short for the post
		}
		return a
	}
	logger := slog.New(slog.NewMultiHandler(
		slog.NewJSONHandler(os.Stdout, &slog.HandlerOptions{ReplaceAttr: noTime}),
		slog.NewTextHandler(os.Stderr, &slog.HandlerOptions{Level: slog.LevelWarn, ReplaceAttr: noTime}),
	))
	logger = logger.With("service", "bookstore-api")

	logger.Info("order placed", "order_id", "ORD-1001", "total_kurus", 69980)
	logger.Warn("stock low", "isbn", "9780141439686", "stock", 1)
}
