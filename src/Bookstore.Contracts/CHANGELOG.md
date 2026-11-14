# Changelog

All notable changes to this project will be documented in this file. See [commit-and-tag-version](https://github.com/absolute-version/commit-and-tag-version) for commit guidelines.

## [2.1.0](https://github.com/example/bookstore/compare/contracts/v2.0.0...contracts/v2.1.0) (2026-11-14)

### Features

* **contracts:** add BookDto.Publisher ([c2a6386](https://github.com/example/bookstore/commit/c2a63864c5c6c8f8c611939cea3b953e5c4f0e15))

## [2.0.0](https://github.com/example/bookstore/compare/contracts/v1.1.0...contracts/v2.0.0) (2026-11-14)

### ⚠ BREAKING CHANGES

* **contracts:** BookDto.Price is removed. Read BookDto.ListPrice.Amount instead; the positional constructor now takes a Money value.

### Features

* **contracts:** remove BookDto.Price ([4bb4dfc](https://github.com/example/bookstore/commit/4bb4dfcf2a6ff27574f8094e4313d5b03301e168))

## [1.1.0](https://github.com/example/bookstore/compare/contracts/v1.0.0...contracts/v1.1.0) (2026-11-14)

### Features

* **contracts:** add Money and BookDto.ListPrice ([766285f](https://github.com/example/bookstore/commit/766285f989e99a17c2e9d0d12592d0e6d5b321de))

### Bug Fixes

* **contracts:** show currency in Money.ToString ([d42aa12](https://github.com/example/bookstore/commit/d42aa125eb9b9607d6cef343db72f7f762e97c04))
