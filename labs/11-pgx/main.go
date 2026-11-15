// Command 11-pgx runs the stock update of sqlstore against PostgreSQL with
// pgx's own interface instead of database/sql.
package main

import (
	"context"
	"errors"
	"fmt"
	"log"
	"os"

	"github.com/jackc/pgx/v5"
	"github.com/jackc/pgx/v5/pgxpool"
)

type book struct {
	ISBN  string
	Title string
	Stock int
}

func main() {
	ctx := context.Background()
	pool, err := pgxpool.New(ctx, os.Getenv("BOOKSTORE_PG")) // a pool, like *sql.DB
	if err != nil {
		log.Fatal(err)
	}
	defer pool.Close()

	_, err = pool.Exec(ctx, `
		CREATE TABLE IF NOT EXISTS books (
		    isbn  char(13)      PRIMARY KEY,
		    title text          NOT NULL,
		    price numeric(10,2) NOT NULL,
		    stock int           NOT NULL CHECK (stock >= 0));
		INSERT INTO books VALUES ('9780141439686', 'Persuasion', 185.00, 3)
		ON CONFLICT (isbn) DO UPDATE SET stock = 3`)
	if err != nil {
		log.Fatal(err)
	}

	for i := 1; i <= 4; i++ {
		var left int
		// $1, $2 instead of @name; RETURNING gives the new value in the same round trip.
		err := pool.QueryRow(ctx, `
			UPDATE books SET stock = stock - $1
			WHERE isbn = $2 AND stock >= $1
			RETURNING stock`, 1, "9780141439686").Scan(&left)
		if errors.Is(err, pgx.ErrNoRows) {
			fmt.Printf("order %d: out of stock\n", i)
			continue
		}
		if err != nil {
			log.Fatal(err)
		}
		fmt.Printf("order %d: placed, %d left\n", i, left)
	}

	rows, _ := pool.Query(ctx, `SELECT isbn, title, stock FROM books`)
	books, err := pgx.CollectRows(rows, pgx.RowToStructByPos[book]) // closes rows for us
	if err != nil {
		log.Fatal(err)
	}
	fmt.Printf("%+v\n", books)
}
