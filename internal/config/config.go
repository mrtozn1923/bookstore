// Package config reads the settings of bookstore-api from environment
// variables, the way a container or Kubernetes passes them.
package config

import (
	"errors"
	"fmt"
	"log/slog"
	"net"
	"net/url"
	"time"
)

// Config holds every setting the service reads at start-up.
type Config struct {
	Addr            string         // BOOKSTORE_ADDR: public API
	DebugAddr       string         // BOOKSTORE_DEBUG_ADDR: pprof; "off" disables it
	CORSOrigin      string         // BOOKSTORE_CORS_ORIGIN: the Angular app
	DatabaseURL     string         // BOOKSTORE_DB: empty keeps orders in memory
	LogFormat       string         // BOOKSTORE_LOG_FORMAT: json or text
	LogLevel        slog.Level     // BOOKSTORE_LOG_LEVEL: debug, info, warn, error
	DrainDelay      time.Duration  // BOOKSTORE_DRAIN_DELAY: /readyz fails this long before shutdown
	ShutdownTimeout time.Duration  // BOOKSTORE_SHUTDOWN_TIMEOUT: time for requests in flight
	WarehouseTZ     *time.Location // BOOKSTORE_WAREHOUSE_TZ: IANA name for delivery estimates
}

// FromEnv builds a Config from getenv (os.Getenv in main, a map in tests).
// It reports every invalid setting at once, not only the first.
func FromEnv(getenv func(string) string) (Config, error) {
	get := func(key, def string) string {
		if v := getenv(key); v != "" {
			return v
		}
		return def
	}
	c := Config{
		Addr:        get("BOOKSTORE_ADDR", "127.0.0.1:52710"),
		DebugAddr:   get("BOOKSTORE_DEBUG_ADDR", "127.0.0.1:52711"),
		CORSOrigin:  get("BOOKSTORE_CORS_ORIGIN", "http://localhost:4200"),
		DatabaseURL: getenv("BOOKSTORE_DB"),
		LogFormat:   get("BOOKSTORE_LOG_FORMAT", "json"),
	}

	var errs []error
	if _, _, err := net.SplitHostPort(c.Addr); err != nil {
		errs = append(errs, fmt.Errorf("BOOKSTORE_ADDR: %w", err))
	}
	if c.DebugAddr == "off" {
		c.DebugAddr = ""
	} else if _, _, err := net.SplitHostPort(c.DebugAddr); err != nil {
		errs = append(errs, fmt.Errorf("BOOKSTORE_DEBUG_ADDR: %w", err))
	}
	if u, err := url.Parse(c.CORSOrigin); err != nil || (u.Scheme != "http" && u.Scheme != "https") || u.Host == "" || u.Path != "" {
		errs = append(errs, fmt.Errorf("BOOKSTORE_CORS_ORIGIN: %q is not an origin like https://shop.example.com", c.CORSOrigin))
	}
	if c.LogFormat != "json" && c.LogFormat != "text" {
		errs = append(errs, fmt.Errorf("BOOKSTORE_LOG_FORMAT: %q is not json or text", c.LogFormat))
	}
	if err := c.LogLevel.UnmarshalText([]byte(get("BOOKSTORE_LOG_LEVEL", "info"))); err != nil {
		errs = append(errs, fmt.Errorf("BOOKSTORE_LOG_LEVEL: %w", err))
	}
	if d, err := time.ParseDuration(get("BOOKSTORE_DRAIN_DELAY", "0s")); err != nil || d < 0 {
		errs = append(errs, fmt.Errorf("BOOKSTORE_DRAIN_DELAY: %q is not a duration like 5s", getenv("BOOKSTORE_DRAIN_DELAY")))
	} else {
		c.DrainDelay = d
	}
	if d, err := time.ParseDuration(get("BOOKSTORE_SHUTDOWN_TIMEOUT", "20s")); err != nil || d <= 0 {
		errs = append(errs, fmt.Errorf("BOOKSTORE_SHUTDOWN_TIMEOUT: %q is not a positive duration like 20s", getenv("BOOKSTORE_SHUTDOWN_TIMEOUT")))
	} else {
		c.ShutdownTimeout = d
	}
	if loc, err := time.LoadLocation(get("BOOKSTORE_WAREHOUSE_TZ", "Europe/Istanbul")); err != nil {
		errs = append(errs, fmt.Errorf("BOOKSTORE_WAREHOUSE_TZ: %w", err))
	} else {
		c.WarehouseTZ = loc
	}
	return c, errors.Join(errs...)
}
