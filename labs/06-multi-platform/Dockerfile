# syntax=docker/dockerfile:1
FROM alpine:3
ARG TARGETPLATFORM
RUN echo "image for $TARGETPLATFORM, RUN executed on $(uname -m)" > /build-info.txt
CMD ["cat", "/build-info.txt"]
