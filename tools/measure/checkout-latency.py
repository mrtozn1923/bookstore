# Post 15: 20 orders through the BFF as Ada; POST latency and time until Ordering reports Confirmed.
# Usage (with `aspire run` running): BOOKSTORE_DEMO_PASSWORD=<keycloak-demo-password parameter> python3 tools/measure/checkout-latency.py [isbn]
# Pick a book with at least 23 copies in stock: an out-of-stock order is cancelled in milliseconds and would skew the numbers.
import collections, html, http.cookiejar, json, os, re, statistics, sys, time, urllib.parse, urllib.request
BFF = "http://localhost:5100"
isbn = sys.argv[1] if len(sys.argv) > 1 else "9780141439518"
jar = http.cookiejar.CookieJar()
# Browsers send Secure cookies to http://localhost; Python's cookie jar does not (post 13 has the same workaround)
class NoSecure(urllib.request.HTTPCookieProcessor):
    def http_response(self, request, response):
        r = super().http_response(request, response)
        for c in jar: c.secure = False
        return r
opener = urllib.request.build_opener(NoSecure(jar))
with opener.open(BFF + "/bff/login?returnUrl=/") as r: page = r.read().decode()
action = html.unescape(re.search(r'id="kc-form-login"[^>]*action="([^"]+)"', page, re.S).group(1))
with opener.open(urllib.request.Request(action, data=urllib.parse.urlencode({"username": "ada", "password": os.environ["BOOKSTORE_DEMO_PASSWORD"]}).encode(), method="POST")) as r: page = r.read().decode()
target = html.unescape(re.search(r'<form[^>]*action="([^"]+)"', page, re.I).group(1))
fields = {html.unescape(n): html.unescape(v) for n, v in re.findall(r'<input[^>]*name="([^"]+)"[^>]*value="([^"]*)"', page, re.I)}
opener.open(urllib.request.Request(target, data=urllib.parse.urlencode(fields).encode(), method="POST")).read()
with opener.open(f"{BFF}/api/books/{isbn}") as r: price = json.loads(r.read())["price"]
post, confirm, outcomes = [], [], collections.Counter()
for i in range(23):
    body = json.dumps({"items": [{"isbn": isbn, "quantity": 1, "unitPrice": price}]}).encode()
    t0 = time.perf_counter()
    with opener.open(urllib.request.Request(BFF + "/api/orders", data=body, method="POST", headers={"content-type": "application/json", "X-CSRF": "1"})) as r:
        loc = r.headers["Location"]; t1 = time.perf_counter()
    while True:
        with opener.open(BFF + loc) as r: status = json.loads(r.read())["status"]
        if status in ("Confirmed", "Cancelled"): break
        time.sleep(0.02)
    t2 = time.perf_counter()
    if i >= 3: post.append((t1 - t0) * 1000); confirm.append((t2 - t0) * 1000); outcomes[status] += 1
    time.sleep(2.1)  # the BFF's rate limit: 5 orders per 10 s per client (post 09)
q = lambda xs: statistics.quantiles(xs, n=20)[18]
print(f"microservices POST /api/orders: n={len(post)} p50={statistics.median(post):.1f} ms p95={q(post):.1f} ms")
print(f"outcomes: {dict(outcomes)}")
print(f"microservices POST to Confirmed: p50={statistics.median(confirm):.0f} ms p95={q(confirm):.0f} ms")
