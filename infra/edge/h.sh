# h <url> [curl args...]: prints status, cache headers and the body
h() {
    local url=$1; shift
    curl -s -D /tmp/h.hdr -o /tmp/h.body -H "Host: bookstore.example" "$@" "$url"
    grep -iE "^(HTTP|cache-control|cdn-cache-control|x-origin-request|x-cache-status|set-cookie)" /tmp/h.hdr | tr -d '\r'
    head -c 160 /tmp/h.body; echo
}
