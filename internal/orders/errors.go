package orders

import "errors"

// Sentinel errors callers can test with errors.Is.
var (
	ErrNotFound          = errors.New("order not found")
	ErrOutOfStock        = errors.New("out of stock")
	ErrInvalidTransition = errors.New("invalid status transition")
)

// ValidationError reports one invalid field of a request.
type ValidationError struct {
	Field   string
	Message string
}

func (e ValidationError) Error() string {
	return e.Field + ": " + e.Message
}
