// Lab 16: the monotonic clock reading and why == is the wrong comparison.
package main

import (
	"encoding/json"
	"fmt"
	"time"
)

func main() {
	now := time.Now()
	fmt.Println(now) // the tail shows the monotonic reading: m=+...

	wall := now.Round(0) // Round(0) strips the monotonic reading
	fmt.Println(now == wall, now.Equal(wall))

	b, _ := json.Marshal(now)
	var back time.Time
	_ = json.Unmarshal(b, &back)
	fmt.Println(string(b), now == back, now.Equal(back))

	seen := map[time.Time]bool{now: true}
	fmt.Println(seen[back], seen[now])

	start := time.Now()
	time.Sleep(20 * time.Millisecond)
	fmt.Println(time.Since(start).Round(time.Millisecond)) // uses the monotonic clock
}
