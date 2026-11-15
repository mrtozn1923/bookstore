//go:build go1.27

// Lab 10: encoding/json (v1) and encoding/json/v2 side by side.
package main

import (
	jsonv1 "encoding/json"
	"encoding/json/jsontext"
	"encoding/json/v2"
	"fmt"
	"time"
)

// shipment shows omitempty and omitzero (Go 1.24) on a time.Time.
type shipment struct {
	ShippedAt   time.Time `json:"shippedAt,omitempty"`  // a struct is never "empty"
	DeliveredAt time.Time `json:"deliveredAt,omitzero"` // the zero time is left out
}

type request struct {
	CustomerID string   `json:"customerId"`
	Coupons    []string `json:"coupons"`
}

func main() {
	// 1) A nil slice.
	empty := request{CustomerID: "ada"}
	b1, _ := jsonv1.Marshal(empty)
	b2, _ := json.Marshal(empty)
	b3, _ := json.Marshal(empty, json.FormatNilSliceAsNull(true))
	fmt.Printf("v1:      %s\nv2:      %s\nv2+null: %s\n", b1, b2, b3)

	// 2) The same key twice, in different case.
	body := []byte(`{"customerId":"ada","CUSTOMERID":"mallory"}`)
	var r1, r2 request
	err1 := jsonv1.Unmarshal(body, &r1)
	err2 := json.Unmarshal(body, &r2)
	fmt.Printf("v1: %q err=%v\nv2: %q err=%v\n", r1.CustomerID, err1, r2.CustomerID, err2)

	// 3) The exact same key twice.
	dup := []byte(`{"customerId":"ada","customerId":"mallory"}`)
	var d1, d2, d3 request
	err1 = jsonv1.Unmarshal(dup, &d1)
	err2 = json.Unmarshal(dup, &d2)
	err3 := json.Unmarshal(dup, &d3, jsontext.AllowDuplicateNames(true))
	fmt.Printf("v1: %q err=%v\nv2: %q err=%v\nv2+allow: %q err=%v\n", d1.CustomerID, err1, d2.CustomerID, err2, d3.CustomerID, err3)

	// 4) Invalid UTF-8 inside a string.
	bad := []byte("{\"customerId\":\"ad\xffa\"}")
	var u1, u2 request
	err1 = jsonv1.Unmarshal(bad, &u1)
	err2 = json.Unmarshal(bad, &u2)
	fmt.Printf("v1: %q err=%v\nv2: %q err=%v\n", u1.CustomerID, err1, u2.CustomerID, err2)

	// 5) omitempty cannot leave out a zero time.Time; omitzero can.
	s1, _ := jsonv1.Marshal(shipment{})
	fmt.Printf("%s\n", s1)
}
