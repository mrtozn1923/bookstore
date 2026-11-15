// Package inventory reads stock levels from Bookstore.Api, the .NET service
// that owns the warehouse data.
package inventory

import (
	"context"
	"encoding/json"
	"errors"
	"fmt"
	"io"
	"math/rand/v2"
	"net"
	"net/http"
	"net/url"
	"strconv"
	"time"
)

// StockLevel is the body of GET /api/stock/{isbn}.
type StockLevel struct {
	ISBN      string `json:"isbn"`
	Available int    `json:"available"`
	Warehouse string `json:"warehouse"`
}

// ErrNotFound is returned when the stock service does not know the ISBN.
var ErrNotFound = errors.New("stock level not found")

// StatusError is an answer with a status code the client does not expect.
type StatusError struct {
	Code       int
	Body       string        // the first bytes, for the log
	RetryAfter time.Duration // from the Retry-After header, 0 if missing
}

func (e *StatusError) Error() string {
	return fmt.Sprintf("stock service: %d %s: %q", e.Code, http.StatusText(e.Code), e.Body)
}

// Client calls the stock service. Create it once and share it: the
// connection pool lives in its Transport.
type Client struct {
	BaseURL string
	HTTP    *http.Client
	Retries int           // extra attempts after the first one
	Backoff time.Duration // wait before the first retry; doubles each time
}

// NewClient returns a Client with explicit timeouts for every phase.
func NewClient(baseURL string) *Client {
	transport := &http.Transport{
		Proxy:                 http.ProxyFromEnvironment,
		DialContext:           (&net.Dialer{Timeout: 2 * time.Second, KeepAlive: 30 * time.Second}).DialContext,
		TLSHandshakeTimeout:   3 * time.Second,
		ResponseHeaderTimeout: 3 * time.Second, // from the end of the request to the response headers
		MaxIdleConns:          100,
		MaxIdleConnsPerHost:   32, // the default is 2
		IdleConnTimeout:       90 * time.Second,
		ForceAttemptHTTP2:     true,
	}
	return &Client{
		BaseURL: baseURL,
		HTTP:    &http.Client{Transport: transport, Timeout: 5 * time.Second}, // one attempt, body included
		Retries: 2,
		Backoff: 100 * time.Millisecond,
	}
}

// Stock returns the stock level of one book. GET is idempotent, so failed
// attempts that may be temporary are retried.
func (c *Client) Stock(ctx context.Context, isbn string) (StockLevel, error) {
	var lvl StockLevel
	err := c.retry(ctx, func() error {
		return c.getJSON(ctx, "/api/stock/"+url.PathEscape(isbn), &lvl)
	})
	return lvl, err
}

func (c *Client) getJSON(ctx context.Context, path string, dst any) error {
	req, err := http.NewRequestWithContext(ctx, http.MethodGet, c.BaseURL+path, nil)
	if err != nil {
		return err
	}
	req.Header.Set("Accept", "application/json")
	resp, err := c.HTTP.Do(req)
	if err != nil {
		return err // no answer: connection, timeout or cancellation
	}
	defer resp.Body.Close() // always, or the connection never goes back to the pool

	switch {
	case resp.StatusCode == http.StatusNotFound:
		return ErrNotFound
	case resp.StatusCode != http.StatusOK: // a 4xx or 5xx is not an error for http.Client
		body, _ := io.ReadAll(io.LimitReader(resp.Body, 256))
		return &StatusError{Code: resp.StatusCode, Body: string(body), RetryAfter: retryAfter(resp.Header)}
	}
	if err := json.NewDecoder(io.LimitReader(resp.Body, 1<<20)).Decode(dst); err != nil {
		return fmt.Errorf("decode stock level: %w", err)
	}
	return nil
}

// retry runs call until it succeeds, fails for good, or ctx ends.
func (c *Client) retry(ctx context.Context, call func() error) error {
	wait := c.Backoff
	for attempt := 0; ; attempt++ {
		err := call()
		if err == nil || attempt == c.Retries || !temporary(err) || ctx.Err() != nil {
			return err
		}
		d := wait / 2
		if wait > 0 {
			d += rand.N(wait) // jitter: callers that failed together do not retry together
		}
		var se *StatusError
		if errors.As(err, &se) && se.RetryAfter > d {
			d = se.RetryAfter // the server knows best how long to wait
		}
		select {
		case <-time.After(d):
		case <-ctx.Done():
			return errors.Join(err, context.Cause(ctx))
		}
		wait *= 2
	}
}

// temporary reports whether another attempt may succeed.
func temporary(err error) bool {
	var se *StatusError
	if errors.As(err, &se) {
		switch se.Code {
		case http.StatusRequestTimeout, http.StatusTooManyRequests, http.StatusInternalServerError,
			http.StatusBadGateway, http.StatusServiceUnavailable, http.StatusGatewayTimeout:
			return true
		}
		return false
	}
	var ue *url.Error // what http.Client.Do returns for transport errors
	return errors.As(err, &ue)
}

// retryAfter reads Retry-After in seconds; the HTTP-date form is ignored.
func retryAfter(h http.Header) time.Duration {
	s, err := strconv.Atoi(h.Get("Retry-After"))
	if err != nil || s < 0 {
		return 0
	}
	return time.Duration(s) * time.Second
}
