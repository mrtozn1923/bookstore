// Lab 19: which timeout fires in which phase of a request.
package main

import (
	"context"
	"fmt"
	"io"
	"net"
	"net/http"
	"time"
)

// slowStock is a stand-in for Bookstore.Api's stock endpoint with slow paths.
func slowStock() http.Handler {
	mux := http.NewServeMux()
	mux.HandleFunc("GET /api/stock/slow-headers", func(w http.ResponseWriter, r *http.Request) {
		time.Sleep(2 * time.Second) // thinks before answering
		fmt.Fprintln(w, `{"available":12}`)
	})
	mux.HandleFunc("GET /api/stock/slow-body", func(w http.ResponseWriter, r *http.Request) {
		w.Header().Set("Content-Type", "application/json")
		w.WriteHeader(http.StatusOK) // headers go out at once
		w.(http.Flusher).Flush()
		time.Sleep(2 * time.Second) // the body comes later
		fmt.Fprintln(w, `{"available":12}`)
	})
	mux.HandleFunc("GET /api/stock/broken", func(w http.ResponseWriter, r *http.Request) {
		http.Error(w, "database unavailable", http.StatusInternalServerError)
	})
	return mux
}

func get(c *http.Client, ctx context.Context, url string) {
	start := time.Now()
	req, _ := http.NewRequestWithContext(ctx, http.MethodGet, url, nil)
	resp, err := c.Do(req)
	status := "no response"
	if err == nil {
		status = resp.Status
		_, err = io.ReadAll(resp.Body) // a timeout can also fire here
		resp.Body.Close()
	}
	fmt.Printf("  %-27s after %v: %v\n", status, time.Since(start).Round(100*time.Millisecond), err)
}

func main() {
	ln, err := net.Listen("tcp", "127.0.0.1:52716")
	if err != nil {
		panic(err)
	}
	srv := &http.Server{Handler: slowStock(), ReadHeaderTimeout: 5 * time.Second}
	go srv.Serve(ln)
	defer srv.Close()
	base := "http://127.0.0.1:52716/api/stock/"

	fmt.Println("http.DefaultClient.Timeout =", http.DefaultClient.Timeout)

	fmt.Println("Client.Timeout = 1s:")
	c := &http.Client{Timeout: time.Second}
	get(c, context.Background(), base+"slow-headers")
	get(c, context.Background(), base+"slow-body")

	fmt.Println("Transport.ResponseHeaderTimeout = 1s:")
	t := http.DefaultTransport.(*http.Transport).Clone()
	t.ResponseHeaderTimeout = time.Second
	c = &http.Client{Transport: t}
	get(c, context.Background(), base+"slow-headers")
	get(c, context.Background(), base+"slow-body") // not covered: the headers were fast

	fmt.Println("context.WithTimeout 1s, no client timeout:")
	ctx, cancel := context.WithTimeout(context.Background(), time.Second)
	defer cancel()
	get(http.DefaultClient, ctx, base+"slow-body")

	fmt.Println("a 500 is not an error:")
	get(http.DefaultClient, context.Background(), base+"broken")

	// A TCP server that accepts connections but never speaks TLS.
	tlsLn, err := net.Listen("tcp", "127.0.0.1:52717")
	if err != nil {
		panic(err)
	}
	defer tlsLn.Close()
	go func() {
		for {
			conn, err := tlsLn.Accept()
			if err != nil {
				return
			}
			go io.Copy(io.Discard, conn) // read the ClientHello, never answer
		}
	}()
	fmt.Println("Transport.TLSHandshakeTimeout = 1s:")
	t = http.DefaultTransport.(*http.Transport).Clone()
	t.TLSHandshakeTimeout = time.Second
	get(&http.Client{Transport: t}, context.Background(), "https://127.0.0.1:52717/api/stock/9780441172719")
}
