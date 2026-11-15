// Command bookstore-api serves the Bookstore HTTP API.
package main

import (
	"context"
	"database/sql"
	"log"
	"net/http"
	"os"
	"time"

	_ "github.com/microsoft/go-mssqldb" // registers the "sqlserver" driver

	"bookstore/internal/catalog"
	"bookstore/internal/httpapi"
	"bookstore/internal/orders"
	"bookstore/internal/orders/memstore"
	"bookstore/internal/orders/sqlstore"
)

const angularDevOrigin = "http://localhost:4200"

func main() {
	books := catalog.Store{}

	var repo orders.Repository = memstore.New()
	if dsn := os.Getenv("BOOKSTORE_DB"); dsn != "" {
		db, err := openDB(dsn, books.All())
		if err != nil {
			log.Fatal(err)
		}
		defer db.Close()
		repo = sqlstore.New(db)
		log.Printf("orders are stored in SQL Server")
	} else {
		log.Printf("BOOKSTORE_DB is not set: orders are kept in memory")
	}

	api := &httpapi.API{
		Books:  books,
		Orders: orders.NewService(repo, books),
	}

	csrf := http.NewCrossOriginProtection()
	if err := csrf.AddTrustedOrigin(angularDevOrigin); err != nil {
		log.Fatal(err)
	}

	handler := httpapi.Chain(api.Routes(),
		httpapi.RequestID, // outermost: runs first, sees every request
		httpapi.Logging,
		httpapi.Recover,
		httpapi.CORS(angularDevOrigin),
		csrf.Handler, // innermost: right before the mux
	)

	srv := &http.Server{
		Addr:              "127.0.0.1:52710",
		Handler:           handler,
		ReadHeaderTimeout: 5 * time.Second,  // request line and headers
		ReadTimeout:       10 * time.Second, // headers and body
		WriteTimeout:      15 * time.Second, // from the end of the headers to the end of the response
		IdleTimeout:       60 * time.Second, // keep-alive connection waiting for the next request
		MaxHeaderBytes:    64 << 10,
	}
	log.Printf("bookstore-api listening on http://%s", srv.Addr)
	if err := srv.ListenAndServe(); err != nil {
		log.Print(err)
	}
}

// openDB creates the pool once for the whole process, checks that the
// server answers and creates the tables.
func openDB(dsn string, books []catalog.Book) (*sql.DB, error) {
	db, err := sql.Open("sqlserver", dsn) // no connection yet, only the pool
	if err != nil {
		return nil, err
	}
	db.SetMaxOpenConns(20)                  // default 0: no limit
	db.SetMaxIdleConns(10)                  // default 2
	db.SetConnMaxLifetime(30 * time.Minute) // default 0: connections live forever
	db.SetConnMaxIdleTime(5 * time.Minute)  // default 0: idle connections are kept

	ctx, cancel := context.WithTimeout(context.Background(), 10*time.Second)
	defer cancel()
	if err := db.PingContext(ctx); err != nil { // the first real connection
		db.Close()
		return nil, err
	}
	if err := sqlstore.EnsureSchema(ctx, db, books); err != nil {
		db.Close()
		return nil, err
	}
	return db, nil
}
