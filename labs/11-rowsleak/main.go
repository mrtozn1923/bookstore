// Command 11-rowsleak forgets rows.Close on a pool of two connections and
// shows what the pool looks like when the third query arrives.
package main

import (
	"context"
	"database/sql"
	"fmt"
	"log"
	"os"
	"time"

	_ "github.com/microsoft/go-mssqldb"
)

func main() {
	db, err := sql.Open("sqlserver", os.Getenv("BOOKSTORE_DB"))
	if err != nil {
		log.Fatal(err)
	}
	defer db.Close()
	db.SetMaxOpenConns(2)

	closeRows := len(os.Args) > 1 && os.Args[1] == "fixed"
	run := func(n int, ctx context.Context) {
		start := time.Now()
		err := firstTitle(ctx, db, closeRows)
		s := db.Stats()
		fmt.Printf("query %d: err=%v after %v | open=%d inUse=%d idle=%d waitCount=%d\n",
			n, err, time.Since(start).Round(100*time.Millisecond), s.OpenConnections, s.InUse, s.Idle, s.WaitCount)
	}

	// A background job: nothing ever cancels these contexts.
	run(1, context.Background())
	run(2, context.Background())

	ctx, cancel := context.WithTimeout(context.Background(), 2*time.Second)
	defer cancel()
	run(3, ctx)
}

// firstTitle reads only the first row. Without rows.Close the rest of the
// result stays on the connection and the connection never goes back to the pool.
func firstTitle(ctx context.Context, db *sql.DB, closeRows bool) error {
	rows, err := db.QueryContext(ctx, `SELECT Title FROM dbo.Books ORDER BY Title`)
	if err != nil {
		return err
	}
	if closeRows {
		defer rows.Close()
	}
	if rows.Next() {
		var title string
		return rows.Scan(&title)
	}
	return rows.Err()
}
