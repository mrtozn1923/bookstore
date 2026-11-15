// Lab 09: what http.Server timeouts do, and a client that sends headers slowly.
//
//	go run ./labs/09-timeouts server     # http.Server with timeouts
//	go run ./labs/09-timeouts plain      # http.ListenAndServe, no timeouts
//	go run ./labs/09-timeouts slowloris  # client: half a request, then silence
package main

import (
	"fmt"
	"io"
	"log"
	"net"
	"net/http"
	"os"
	"time"
)

const addr = "127.0.0.1:52711"

func routes() *http.ServeMux {
	slow := http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		time.Sleep(2 * time.Second) // a report that takes too long
		fmt.Fprintln(w, "report ready")
	})
	mux := http.NewServeMux()
	mux.Handle("GET /slow", slow)
	mux.Handle("GET /slow-guarded", http.TimeoutHandler(slow, 500*time.Millisecond, "report timed out\n"))
	return mux
}

func main() {
	if len(os.Args) < 2 {
		log.Fatal("usage: server | plain | slowloris")
	}
	switch os.Args[1] {
	case "server":
		srv := &http.Server{
			Addr:              addr,
			Handler:           routes(),
			ReadHeaderTimeout: 2 * time.Second,
			WriteTimeout:      1 * time.Second,
		}
		log.Fatal(srv.ListenAndServe())
	case "plain":
		log.Fatal(http.ListenAndServe(addr, routes()))
	case "slowloris":
		conn, err := net.Dial("tcp", addr)
		if err != nil {
			log.Fatal(err)
		}
		defer conn.Close()
		start := time.Now()
		fmt.Fprint(conn, "GET /slow HTTP/1.1\r\nHost: bookstore\r\n") // no blank line: headers never end
		if err := conn.SetReadDeadline(time.Now().Add(8 * time.Second)); err != nil {
			log.Fatal(err)
		}
		reply, err := io.ReadAll(conn)
		fmt.Printf("after %v: %d bytes, err=%v\n", time.Since(start).Round(100*time.Millisecond), len(reply), err)
		if len(reply) > 0 {
			fmt.Printf("%q\n", reply)
		}
	}
}
