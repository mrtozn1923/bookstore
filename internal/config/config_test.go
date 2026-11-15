package config

import (
	"log/slog"
	"strings"
	"testing"
	"time"
)

func env(m map[string]string) func(string) string {
	return func(k string) string { return m[k] }
}

func TestFromEnvDefaults(t *testing.T) {
	c, err := FromEnv(env(nil))
	if err != nil {
		t.Fatal(err)
	}
	want := Config{Addr: "127.0.0.1:52710", DebugAddr: "127.0.0.1:52711", CORSOrigin: "http://localhost:4200",
		LogFormat: "json", LogLevel: slog.LevelInfo, ShutdownTimeout: 20 * time.Second}
	if c != want {
		t.Errorf("FromEnv() = %+v, want %+v", c, want)
	}
}

func TestFromEnv(t *testing.T) {
	tests := []struct {
		name    string
		env     map[string]string
		check   func(Config) bool
		wantErr []string // every substring must appear in the error
	}{
		{"container", map[string]string{"BOOKSTORE_ADDR": ":8080", "BOOKSTORE_DEBUG_ADDR": "off"},
			func(c Config) bool { return c.Addr == ":8080" && c.DebugAddr == "" }, nil},
		{"kubernetes", map[string]string{"BOOKSTORE_DRAIN_DELAY": "5s", "BOOKSTORE_SHUTDOWN_TIMEOUT": "25s"},
			func(c Config) bool { return c.DrainDelay == 5*time.Second && c.ShutdownTimeout == 25*time.Second }, nil},
		{"debug level", map[string]string{"BOOKSTORE_LOG_LEVEL": "debug", "BOOKSTORE_LOG_FORMAT": "text"},
			func(c Config) bool { return c.LogLevel == slog.LevelDebug && c.LogFormat == "text" }, nil},
		{"all wrong", map[string]string{"BOOKSTORE_ADDR": "8080", "BOOKSTORE_CORS_ORIGIN": "localhost:4200",
			"BOOKSTORE_LOG_LEVEL": "loud", "BOOKSTORE_SHUTDOWN_TIMEOUT": "20"},
			nil, []string{"BOOKSTORE_ADDR", "BOOKSTORE_CORS_ORIGIN", "BOOKSTORE_LOG_LEVEL", "BOOKSTORE_SHUTDOWN_TIMEOUT"}},
	}
	for _, tt := range tests {
		t.Run(tt.name, func(t *testing.T) {
			c, err := FromEnv(env(tt.env))
			if tt.wantErr == nil {
				if err != nil || !tt.check(c) {
					t.Fatalf("FromEnv() = %+v, %v", c, err)
				}
				return
			}
			for _, w := range tt.wantErr {
				if err == nil || !strings.Contains(err.Error(), w) {
					t.Errorf("error %v does not mention %s", err, w)
				}
			}
		})
	}
}
