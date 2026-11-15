package httpapi

import (
	"encoding/json"
	"errors"
	"fmt"
	"io"
	"mime"
	"net/http"
	"time"

	"bookstore/internal/catalog"
	"bookstore/internal/orders"
)

// maxBodyBytes caps request bodies; an order is a few hundred bytes.
const maxBodyBytes = 64 << 10

// placeOrderRequest is the body of POST /api/orders.
type placeOrderRequest struct {
	CustomerID string             `json:"customerId"`
	Items      []orderItemRequest `json:"items"`
}

type orderItemRequest struct {
	ISBN     string `json:"isbn"`
	Quantity *int   `json:"quantity"` // nil: the field was missing, not 0
}

// orderResponse is what the API returns for an order.
type orderResponse struct {
	ID         string              `json:"id"`
	CustomerID string              `json:"customerId"`
	Status     string              `json:"status"`
	Items      []orderItemResponse `json:"items"`
	Total      catalog.Money       `json:"total"` // in kuruş, like every price
	CreatedAt  time.Time           `json:"createdAt"`
}

type orderItemResponse struct {
	ISBN      string        `json:"isbn"`
	Title     string        `json:"title"`
	UnitPrice catalog.Money `json:"unitPrice"`
	Quantity  int           `json:"quantity"`
}

func newOrderResponse(o *orders.Order) orderResponse {
	items := make([]orderItemResponse, 0, len(o.Items)) // never nil: [] not null
	for _, it := range o.Items {
		items = append(items, orderItemResponse{ISBN: it.ISBN, Title: it.Title, UnitPrice: it.UnitPrice, Quantity: it.Quantity})
	}
	return orderResponse{ID: o.ID, CustomerID: o.CustomerID, Status: o.Status.String(),
		Items: items, Total: o.Total(), CreatedAt: o.CreatedAt.UTC()}
}

func (a *API) placeOrder(w http.ResponseWriter, r *http.Request) {
	var req placeOrderRequest
	if err := decodeJSON(w, r, &req); err != nil {
		writeDecodeError(w, r, err)
		return
	}

	lines := make([]orders.Line, 0, len(req.Items))
	var missing []orders.ValidationError
	for i, it := range req.Items {
		if it.Quantity == nil {
			missing = append(missing, orders.ValidationError{Field: fmt.Sprintf("items[%d].quantity", i), Message: "is required"})
			continue
		}
		lines = append(lines, orders.Line{ISBN: it.ISBN, Quantity: *it.Quantity})
	}
	if len(missing) > 0 {
		writeValidationProblem(w, r, missing)
		return
	}

	o, err := a.Orders.Place(r.Context(), req.CustomerID, lines)
	if verrs := orders.ValidationErrors(err); len(verrs) > 0 {
		writeValidationProblem(w, r, verrs)
		return
	}
	switch {
	case errors.Is(err, catalog.ErrBookNotFound):
		writeProblem(w, r, Problem{Status: http.StatusUnprocessableEntity, Detail: err.Error()})
		return
	case errors.Is(err, orders.ErrOutOfStock):
		writeProblem(w, r, Problem{Status: http.StatusConflict, Detail: err.Error()})
		return
	case err != nil:
		a.logger().ErrorContext(r.Context(), "place order", "error", err)
		writeProblem(w, r, Problem{Status: http.StatusInternalServerError})
		return
	}

	w.Header().Set("Location", "/api/orders/"+o.ID)
	w.Header().Set("Content-Type", "application/json")
	w.WriteHeader(http.StatusCreated)
	if err := json.NewEncoder(w).Encode(newOrderResponse(o)); err != nil {
		a.logger().WarnContext(r.Context(), "write json", "error", err)
	}
}

func (a *API) getOrder(w http.ResponseWriter, r *http.Request) {
	o, err := a.Orders.ByID(r.Context(), r.PathValue("id"))
	if errors.Is(err, orders.ErrNotFound) {
		writeProblem(w, r, Problem{Status: http.StatusNotFound, Detail: err.Error()})
		return
	}
	if err != nil {
		a.logger().ErrorContext(r.Context(), "get order", "error", err)
		writeProblem(w, r, Problem{Status: http.StatusInternalServerError})
		return
	}
	writeJSON(w, newOrderResponse(o))
}

// errUnsupportedMediaType is returned when the body is not JSON.
var errUnsupportedMediaType = errors.New("Content-Type must be application/json")

// decodeJSON reads exactly one JSON object from a body of at most maxBodyBytes
// into dst and rejects fields that dst does not have.
func decodeJSON(w http.ResponseWriter, r *http.Request, dst any) error {
	if mt, _, err := mime.ParseMediaType(r.Header.Get("Content-Type")); err != nil || mt != "application/json" {
		return errUnsupportedMediaType
	}
	r.Body = http.MaxBytesReader(w, r.Body, maxBodyBytes)
	dec := json.NewDecoder(r.Body)
	dec.DisallowUnknownFields()
	if err := dec.Decode(dst); err != nil {
		return err
	}
	if err := dec.Decode(&struct{}{}); !errors.Is(err, io.EOF) {
		return errors.New("body must contain a single JSON object")
	}
	return nil
}

// writeDecodeError maps a decodeJSON error to a problem response.
func writeDecodeError(w http.ResponseWriter, r *http.Request, err error) {
	var tooLarge *http.MaxBytesError
	switch {
	case errors.Is(err, errUnsupportedMediaType):
		writeProblem(w, r, Problem{Status: http.StatusUnsupportedMediaType, Detail: err.Error()})
	case errors.As(err, &tooLarge):
		writeProblem(w, r, Problem{Status: http.StatusRequestEntityTooLarge,
			Detail: fmt.Sprintf("body must be at most %d bytes", tooLarge.Limit)})
	default:
		writeProblem(w, r, Problem{Status: http.StatusBadRequest, Detail: err.Error()})
	}
}

// writeValidationProblem lists every invalid field, in the shape of
// ASP.NET Core's ValidationProblemDetails.
func writeValidationProblem(w http.ResponseWriter, r *http.Request, verrs []orders.ValidationError) {
	fields := make(map[string][]string, len(verrs))
	for _, v := range verrs {
		fields[v.Field] = append(fields[v.Field], v.Message)
	}
	writeProblem(w, r, Problem{Status: http.StatusBadRequest,
		Title: "One or more validation errors occurred.", Errors: fields})
}
