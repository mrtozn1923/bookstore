//go:build go1.27

// Lab 16: time.Duration in JSON, v1 and v2.
package main

import (
	jsonv1 "encoding/json"
	"encoding/json/v2"
	"fmt"
	"time"
)

type retryPolicy struct {
	Delay time.Duration `json:"delay"`
}

type retryPolicyFormat struct {
	Delay time.Duration `json:"delay,format:units"` // accepted during the experiment
}

func main() {
	p := retryPolicy{Delay: 1500 * time.Millisecond}
	b, err := jsonv1.Marshal(p)
	fmt.Println("v1:      ", string(b), err)
	b, err = json.Marshal(p)
	fmt.Println("v2:      ", string(b), err)
	b, err = json.Marshal(p, jsonv1.FormatDurationAsNano(true))
	fmt.Println("v2 nano: ", string(b), err)
	b, err = json.Marshal(retryPolicyFormat(p))
	fmt.Println("v2 tag:  ", string(b), err)
	fmt.Println("String():", p.Delay) // what ParseDuration reads back
}
