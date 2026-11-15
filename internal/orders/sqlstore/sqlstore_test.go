package sqlstore_test

import (
	"database/sql"
	"errors"
	"os"
	"testing"
	"time"

	_ "github.com/microsoft/go-mssqldb"

	"bookstore/internal/catalog"
	"bookstore/internal/orders"
	"bookstore/internal/orders/sqlstore"
)

// openTestDB connects to the database in BOOKSTORE_TEST_DB and skips the
// test when it is not set, so "go test ./..." also works without SQL Server.
func openTestDB(t *testing.T) *sql.DB {
	t.Helper()
	dsn := os.Getenv("BOOKSTORE_TEST_DB")
	if dsn == "" {
		t.Skip("BOOKSTORE_TEST_DB is not set")
	}
	db, err := sql.Open("sqlserver", dsn)
	if err != nil {
		t.Fatal(err)
	}
	t.Cleanup(func() { db.Close() })
	if err := sqlstore.EnsureSchema(t.Context(), db, catalog.Store{}.All()); err != nil {
		t.Fatal(err)
	}
	return db
}

func TestSaveAndLoad(t *testing.T) {
	db := openTestDB(t)
	const emma = "9780141439587"
	if _, err := db.ExecContext(t.Context(), `UPDATE dbo.Books SET Stock = 2 WHERE Isbn = @p1`, emma); err != nil {
		t.Fatal(err)
	}
	store := sqlstore.New(db)
	svc := orders.NewService(store, catalog.Store{})

	placed, err := svc.Place(t.Context(), "ada", []orders.Line{{ISBN: emma, Quantity: 2}})
	if err != nil {
		t.Fatal(err)
	}
	got, err := store.ByID(t.Context(), placed.ID)
	if err != nil {
		t.Fatal(err)
	}
	// datetime2(3) rounds to milliseconds; otherwise it must be the same instant.
	if d := got.CreatedAt.Sub(placed.CreatedAt).Abs(); d > time.Millisecond {
		t.Errorf("CreatedAt = %v, want %v (off by %v)", got.CreatedAt, placed.CreatedAt, d)
	}
	if got.Total() != 39800 || len(got.Items) != 1 || got.PaidAt != nil {
		t.Errorf("loaded %v, PaidAt %v; want 1 item, 398.00 TRY, not paid", got, got.PaidAt)
	}

	_, err = svc.Place(t.Context(), "grace", []orders.Line{{ISBN: emma, Quantity: 1}})
	if !errors.Is(err, orders.ErrOutOfStock) {
		t.Fatalf("second order error = %v, want ErrOutOfStock", err)
	}
}

func TestByIDNotFound(t *testing.T) {
	_, err := sqlstore.New(openTestDB(t)).ByID(t.Context(), "ORD-0")
	if !errors.Is(err, orders.ErrNotFound) {
		t.Fatalf("error = %v, want ErrNotFound", err)
	}
}
