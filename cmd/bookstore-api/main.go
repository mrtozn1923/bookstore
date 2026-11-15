// Command bookstore-api serves the Bookstore HTTP API.
package main

import (
	"context"
	"database/sql"
	"errors"
	"fmt"
	"io"
	"log/slog"
	"net/http"
	"net/http/pprof"
	"os"
	"os/signal"
	"runtime"
	"runtime/debug"
	"syscall"
	"time"

	_ "github.com/microsoft/go-mssqldb" // registers the "sqlserver" driver

	"bookstore/internal/catalog"
	"bookstore/internal/config"
	"bookstore/internal/httpapi"
	"bookstore/internal/orders"
	"bookstore/internal/orders/memstore"
	"bookstore/internal/orders/sqlstore"
)

// version is set at build time: go build -ldflags "-X main.version=1.13.0".
var version = "dev"

func main() {
	// The context is cancelled on Ctrl+C and on SIGTERM, which is what
	// docker stop and Kubernetes send before they kill the process.
	ctx, stop := signal.NotifyContext(context.Background(), os.Interrupt, syscall.SIGTERM)
	defer stop()
	if err := run(ctx, os.Getenv, os.Stdout); err != nil {
		fmt.Fprintln(os.Stderr, "bookstore-api:", err)
		os.Exit(1)
	}
}

func run(ctx context.Context, getenv func(string) string, out io.Writer) error {
	cfg, err := config.FromEnv(getenv)
	if err != nil {
		return err
	}
	logger := newLogger(cfg, out)
	slog.SetDefault(logger) // the log package and slog.Info now go through it too

	logger.Info("starting",
		"version", version,
		"go", runtime.Version(),
		"gomaxprocs", runtime.GOMAXPROCS(0),
		"memory_limit", debug.SetMemoryLimit(-1), // -1 reads the limit without changing it
	)

	books := catalog.Store{}
	health := &httpapi.Health{}
	var repo orders.Repository = memstore.New()
	if cfg.DatabaseURL != "" {
		db, err := openDB(ctx, cfg.DatabaseURL, books.All())
		if err != nil {
			return err
		}
		defer db.Close()
		repo = sqlstore.New(db)
		health.Check = db.PingContext
	}

	api := &httpapi.API{Books: books, Orders: orders.NewService(repo, books), Health: health, Log: logger}
	csrf := http.NewCrossOriginProtection()
	if err := csrf.AddTrustedOrigin(cfg.CORSOrigin); err != nil {
		return err
	}
	handler := httpapi.Chain(api.Routes(),
		httpapi.RequestID, // outermost: runs first, sees every request
		httpapi.Logging(logger),
		httpapi.Recover(logger),
		httpapi.CORS(cfg.CORSOrigin),
		csrf.Handler, // innermost: right before the mux
	)

	srv := &http.Server{
		Addr:              cfg.Addr,
		Handler:           handler,
		ReadHeaderTimeout: 5 * time.Second,  // request line and headers
		ReadTimeout:       10 * time.Second, // headers and body
		WriteTimeout:      15 * time.Second, // from the end of the headers to the end of the response
		IdleTimeout:       60 * time.Second, // keep-alive connection waiting for the next request
		MaxHeaderBytes:    64 << 10,
		ErrorLog:          slog.NewLogLogger(logger.Handler(), slog.LevelWarn),
	}
	servers := []*http.Server{srv}
	if cfg.DebugAddr != "" {
		servers = append(servers, &http.Server{
			Addr:              cfg.DebugAddr,
			Handler:           debugMux(),
			ReadHeaderTimeout: 5 * time.Second, // no WriteTimeout: a CPU profile runs for 30s by default
		})
	}

	errc := make(chan error, len(servers))
	for _, s := range servers {
		go func() {
			logger.Info("listening", "addr", s.Addr)
			if err := s.ListenAndServe(); !errors.Is(err, http.ErrServerClosed) {
				errc <- err
			}
		}()
	}

	select {
	case err := <-errc: // e.g. the port is already in use
		return err
	case <-ctx.Done():
	}

	// Graceful shutdown: fail readiness and give the load balancer
	// DrainDelay to notice, then stop accepting connections and let requests
	// in flight finish, but not longer than ShutdownTimeout.
	logger.Info("shutting down", "drain_delay", cfg.DrainDelay.String(), "timeout", cfg.ShutdownTimeout.String())
	health.Drain()
	time.Sleep(cfg.DrainDelay) // the server keeps serving meanwhile
	shutdownCtx, cancel := context.WithTimeout(context.Background(), cfg.ShutdownTimeout)
	defer cancel()
	var errs []error
	for _, s := range servers {
		errs = append(errs, s.Shutdown(shutdownCtx))
	}
	if err := errors.Join(errs...); err != nil {
		return fmt.Errorf("shutdown: %w", err)
	}
	logger.Info("stopped")
	return nil
}

func newLogger(cfg config.Config, out io.Writer) *slog.Logger {
	opts := &slog.HandlerOptions{Level: cfg.LogLevel}
	var h slog.Handler = slog.NewJSONHandler(out, opts)
	if cfg.LogFormat == "text" {
		h = slog.NewTextHandler(out, opts)
	}
	return slog.New(httpapi.NewLogHandler(h))
}

// debugMux serves the profiles of net/http/pprof on their own mux. The
// package also registers them on http.DefaultServeMux, which we never serve.
func debugMux() *http.ServeMux {
	mux := http.NewServeMux()
	mux.HandleFunc("GET /debug/pprof/", pprof.Index) // heap, goroutine, goroutineleak, allocs...
	mux.HandleFunc("GET /debug/pprof/profile", pprof.Profile)
	mux.HandleFunc("GET /debug/pprof/symbol", pprof.Symbol)
	mux.HandleFunc("GET /debug/pprof/trace", pprof.Trace)
	return mux
}

// openDB creates the pool once for the whole process, checks that the
// server answers and creates the tables.
func openDB(ctx context.Context, dsn string, books []catalog.Book) (*sql.DB, error) {
	db, err := sql.Open("sqlserver", dsn) // no connection yet, only the pool
	if err != nil {
		return nil, err
	}
	db.SetMaxOpenConns(20)                  // default 0: no limit
	db.SetMaxIdleConns(10)                  // default 2
	db.SetConnMaxLifetime(30 * time.Minute) // default 0: connections live forever
	db.SetConnMaxIdleTime(5 * time.Minute)  // default 0: idle connections are kept

	ctx, cancel := context.WithTimeout(ctx, 10*time.Second)
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
