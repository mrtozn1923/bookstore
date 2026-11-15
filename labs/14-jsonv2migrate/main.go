//go:build go1.27

// Command 14-jsonv2migrate sends the bodies from post 10 through the v1
// decoder that internal/httpapi uses today and through its json/v2
// counterpart, to see what a migration would change for clients.
package main

import (
	"bytes"
	"errors"
	"fmt"
	"io"
	"strings"

	jsonv1 "encoding/json"
	"encoding/json/v2"
)

type placeOrderRequest struct {
	CustomerID string `json:"customerId"`
	Items      []struct {
		ISBN     string `json:"isbn"`
		Quantity *int   `json:"quantity"`
	} `json:"items"`
}

// decodeV1 is the body of httpapi.decodeJSON without the HTTP parts.
func decodeV1(body []byte, dst any) error {
	dec := jsonv1.NewDecoder(bytes.NewReader(body))
	dec.DisallowUnknownFields()
	if err := dec.Decode(dst); err != nil {
		return err
	}
	if err := dec.Decode(&struct{}{}); !errors.Is(err, io.EOF) {
		return errors.New("body must contain a single JSON object")
	}
	return nil
}

// decodeV2 does the same with json/v2. UnmarshalRead reads the whole input
// and rejects anything after the first value by itself.
func decodeV2(body []byte, dst any) error {
	return json.UnmarshalRead(bytes.NewReader(body), dst, json.RejectUnknownMembers(true))
}

func main() {
	bodies := []string{
		`{"customerId":"ada","items":[{"isbn":"9780441172719","quantity":1}]}`,
		`{"customerId":"ada","CUSTOMERID":"mallory","items":[]}`,
		`{"customerId":"ada","customerId":"mallory","items":[]}`,
		`{"customerId":"ad` + "\xff" + `a","items":[]}`,
		`{"customerId":"ada","coupon":"BOOK10","items":[]}`,
		`{"customerId":"ada","items":[]} {"customerId":"grace"}`,
	}
	for i, b := range bodies {
		var r1, r2 placeOrderRequest
		fmt.Printf("body %d\n  v1: %s\n  v2: %s\n", i+1, result(decodeV1([]byte(b), &r1), r1), result(decodeV2([]byte(b), &r2), r2))
	}
}

func result(err error, r placeOrderRequest) string {
	if err != nil {
		return "error: " + strings.TrimPrefix(err.Error(), "json: ")
	}
	return fmt.Sprintf("ok, customerId=%q", r.CustomerID)
}
