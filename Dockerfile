# syntax=docker/dockerfile:1
# Build stage: runs on the build machine's own platform and cross-compiles
# for the target platform, so no emulation is needed during the build.
FROM --platform=$BUILDPLATFORM golang:1.27.4 AS build
ARG TARGETOS TARGETARCH VERSION=dev
WORKDIR /src
COPY go.mod go.sum ./
RUN go mod download
COPY . .
RUN CGO_ENABLED=0 GOOS=$TARGETOS GOARCH=$TARGETARCH \
    go build -trimpath -ldflags="-s -w -X main.version=$VERSION" -o /out/bookstore-api ./cmd/bookstore-api

# Run stage: no shell, no package manager, runs as a non-root user.
FROM gcr.io/distroless/static-debian13:nonroot
COPY --from=build /out/bookstore-api /bookstore-api
ENV BOOKSTORE_ADDR=:8080
EXPOSE 8080
ENTRYPOINT ["/bookstore-api"]
