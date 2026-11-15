// Lab 04: value receivers work on a copy, pointer receivers on the original.
package main

import "fmt"

type Order struct {
	ID     string
	Status string
}

func (o Order) PayOnCopy() {
	//lint:ignore SA4005 this lab shows the trap on purpose
	o.Status = "Paid"
}

func (o *Order) Pay() {
	o.Status = "Paid"
}

func main() {
	o := Order{ID: "ORD-1001", Status: "Pending"}
	o.PayOnCopy()
	fmt.Println(o.Status)
	o.Pay() // Go takes &o for us
	fmt.Println(o.Status)
}
