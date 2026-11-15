// Command bookstore-api serves the Bookstore HTTP API.
package main

import (
	"log"
	"net/http"
	"time"

	"bookstore/internal/catalog"
	"bookstore/internal/httpapi"
	"bookstore/internal/orders"
	"bookstore/internal/orders/memstore"
)

const angularDevOrigin = "http://localhost:4200"

func main() {
	books := catalog.Store{}
	api := &httpapi.API{
		Books:  books,
		Orders: orders.NewService(memstore.New(), books),
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
	log.Fatal(srv.ListenAndServe())
}
