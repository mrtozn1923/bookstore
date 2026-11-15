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

// ValidationErrors returns every ValidationError in err's tree, in order.
// It walks both single wrapping (%w) and errors.Join.
func ValidationErrors(err error) []ValidationError {
	var out []ValidationError
	var walk func(error)
	walk = func(e error) {
		switch x := e.(type) {
		case nil:
		case ValidationError:
			out = append(out, x)
		case interface{ Unwrap() []error }:
			for _, inner := range x.Unwrap() {
				walk(inner)
			}
		case interface{ Unwrap() error }:
			walk(x.Unwrap())
		}
	}
	walk(err)
	return out
}
