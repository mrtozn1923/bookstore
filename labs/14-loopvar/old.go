//go:build go1.21

// The build line above sets this file's language version to Go 1.21, so its
// loops still share one variable across iterations.

package main

func titlesGo121() []string {
	var fs []func() string
	for _, t := range catalog {
		fs = append(fs, func() string { return t })
	}
	var out []string
	for _, f := range fs {
		out = append(out, f())
	}
	return out
}
