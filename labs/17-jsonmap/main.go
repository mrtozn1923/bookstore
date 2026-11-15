//go:build go1.27

// Lab 17: v1 sorts map keys in JSON, v2 does so only when asked.
package main

import (
	jsonv1 "encoding/json"
	"encoding/json/v2"
	"fmt"
)

func main() {
	stock := map[string]int{"Dune": 12, "Emma": 7, "Foundation": 9, "Middlemarch": 4, "Persuasion": 3}
	for range 3 {
		v1, _ := jsonv1.Marshal(stock)
		v2, _ := json.Marshal(stock)
		det, _ := json.Marshal(stock, json.Deterministic(true))
		fmt.Printf("v1 %s\nv2 %s\nv2 deterministic %s\n", v1, v2, det)
	}
}
