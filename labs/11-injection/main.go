// Command 11-injection searches books by title twice: once by pasting the
// input into the SQL text, once with a parameter.
package main

import (
	"context"
	"database/sql"
	"fmt"
	"log"
	"os"

	_ "github.com/microsoft/go-mssqldb"
)

func main() {
	db, err := sql.Open("sqlserver", os.Getenv("BOOKSTORE_DB"))
	if err != nil {
		log.Fatal(err)
	}
	defer db.Close()
	ctx := context.Background()

	input := "x' OR 1=1 --" // what an attacker types into the search box

	unsafe := "SELECT Title FROM dbo.Books WHERE Title LIKE '%" + input + "%'"
	fmt.Println("concatenated:", titles(ctx, db, unsafe))

	safe := "SELECT Title FROM dbo.Books WHERE Title LIKE '%' + @q + '%'"
	fmt.Println("parameter:   ", titles(ctx, db, safe, sql.Named("q", input)))
}

func titles(ctx context.Context, db *sql.DB, query string, args ...any) []string {
	rows, err := db.QueryContext(ctx, query, args...)
	if err != nil {
		log.Fatal(err)
	}
	defer rows.Close()
	out := []string{}
	for rows.Next() {
		var t string
		if err := rows.Scan(&t); err != nil {
			log.Fatal(err)
		}
		out = append(out, t)
	}
	if err := rows.Err(); err != nil {
		log.Fatal(err)
	}
	return out
}
