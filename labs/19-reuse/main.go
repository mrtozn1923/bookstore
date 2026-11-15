// Lab 19: when does http.Client reuse a connection?
package main

import (
	"fmt"
	"io"
	"net"
	"net/http"
	"runtime"
	"strings"
	"sync"
	"sync/atomic"
	"time"
)

var newConns atomic.Int64 // counted by the server

var shared = &http.Client{Timeout: 5 * time.Second}

func server() *http.Server {
	small := strings.Repeat("x", 1<<10)
	big := strings.Repeat("x", 1<<20)
	mux := http.NewServeMux()
	mux.HandleFunc("GET /api/stock/small", func(w http.ResponseWriter, r *http.Request) { io.WriteString(w, small) })
	mux.HandleFunc("GET /api/stock/big", func(w http.ResponseWriter, r *http.Request) { io.WriteString(w, big) })
	return &http.Server{
		Handler:           mux,
		ReadHeaderTimeout: 5 * time.Second,
		ConnState: func(_ net.Conn, s http.ConnState) {
			if s == http.StateNew {
				newConns.Add(1)
			}
		},
	}
}

// run sends 50 requests one after another and reports new connections.
func run(name string, client func() *http.Client, path string, handle func(*http.Response)) {
	shared.CloseIdleConnections() // every run starts without idle connections
	time.Sleep(100 * time.Millisecond)
	newConns.Store(0)
	before := runtime.NumGoroutine()
	for range 50 {
		resp, err := client().Get("http://127.0.0.1:52716/api/stock/" + path)
		if err != nil {
			panic(err)
		}
		handle(resp)
	}
	time.Sleep(100 * time.Millisecond) // let the counters settle
	fmt.Printf("%-34s %2d connections, %+3d goroutines\n", name, newConns.Load(), runtime.NumGoroutine()-before)
}

func main() {
	ln, err := net.Listen("tcp", "127.0.0.1:52716")
	if err != nil {
		panic(err)
	}
	srv := server()
	go srv.Serve(ln)
	defer srv.Close()

	same := func() *http.Client { return shared }
	readAndClose := func(r *http.Response) { io.Copy(io.Discard, r.Body); r.Body.Close() }
	closeOnly := func(r *http.Response) { r.Body.Close() }

	run("read + Close, 1 KB", same, "small", readAndClose)
	run("Close without reading, 1 KB", same, "small", closeOnly)
	run("Close without reading, 1 MB", same, "big", closeOnly)
	run("new Transport per request", func() *http.Client {
		return &http.Client{Transport: &http.Transport{}, Timeout: 5 * time.Second}
	}, "small", readAndClose)
	run("no Close at all, 1 KB", same, "small", func(*http.Response) {})

	for _, perHost := range []int{0, 20} { // 0 means the default, 2
		t := &http.Transport{MaxIdleConnsPerHost: perHost}
		c := &http.Client{Transport: t, Timeout: 5 * time.Second}
		newConns.Store(0)
		for range 5 { // five bursts of 20 parallel requests
			var wg sync.WaitGroup
			for range 20 {
				wg.Go(func() {
					resp, err := c.Get("http://127.0.0.1:52716/api/stock/small")
					if err != nil {
						panic(err)
					}
					readAndClose(resp)
				})
			}
			wg.Wait()
		}
		fmt.Printf("100 requests, 20 at a time, MaxIdleConnsPerHost=%-2d %3d connections\n", perHost, newConns.Load())
	}
}
