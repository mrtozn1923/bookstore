package httpapi

import (
	"encoding/json"
	"log"
	"net/http"
)

// Problem is an RFC 9457 problem details body. Errors and TraceID are
// extension members, named as ASP.NET Core names them.
type Problem struct {
	Type     string              `json:"type"`
	Title    string              `json:"title"`
	Status   int                 `json:"status"`
	Detail   string              `json:"detail,omitempty"`
	Instance string              `json:"instance,omitempty"`
	Errors   map[string][]string `json:"errors,omitempty"`
	TraceID  string              `json:"traceId,omitempty"`
}

// problemTypes uses the same type URIs as ASP.NET Core, so the Angular
// client sees one contract from both APIs.
var problemTypes = map[int]string{
	http.StatusBadRequest:            "https://tools.ietf.org/html/rfc9110#section-15.5.1",
	http.StatusNotFound:              "https://tools.ietf.org/html/rfc9110#section-15.5.5",
	http.StatusConflict:              "https://tools.ietf.org/html/rfc9110#section-15.5.10",
	http.StatusRequestEntityTooLarge: "https://tools.ietf.org/html/rfc9110#section-15.5.14",
	http.StatusUnsupportedMediaType:  "https://tools.ietf.org/html/rfc9110#section-15.5.16",
	http.StatusUnprocessableEntity:   "https://tools.ietf.org/html/rfc4918#section-11.2",
	http.StatusInternalServerError:   "https://tools.ietf.org/html/rfc9110#section-15.6.1",
}

// writeProblem fills the common members and writes p as application/problem+json.
func writeProblem(w http.ResponseWriter, r *http.Request, p Problem) {
	if p.Type == "" {
		p.Type = problemTypes[p.Status]
	}
	if p.Type == "" {
		p.Type = "about:blank"
	}
	if p.Title == "" {
		p.Title = http.StatusText(p.Status)
	}
	p.Instance = r.URL.Path
	p.TraceID = RequestIDFrom(r.Context())
	w.Header().Set("Content-Type", "application/problem+json")
	w.WriteHeader(p.Status)
	if err := json.NewEncoder(w).Encode(p); err != nil {
		log.Printf("write problem: %v", err)
	}
}
