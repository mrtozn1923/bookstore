// Which CSS files does the running Bookstore.Web serve?
const base = process.argv[2] ?? "http://127.0.0.1:52861";
for (const path of ["/css/site.css", "/css/input.css"]) {
    const res = await fetch(base + path);
    console.log(res.status, path);
}
