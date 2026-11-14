# seed-emulator.py FILE : PUT every item of an appconfig/kvset file into the local App Configuration emulator (anonymous Owner)
import json, sys, urllib.request, urllib.parse
for it in json.load(open(sys.argv[1]))["items"]:
    q = "?label=" + urllib.parse.quote(it["label"]) if it.get("label") else ""
    body = {"value": it["value"]}
    if it.get("content_type"): body["content_type"] = it["content_type"]
    req = urllib.request.Request("http://localhost:8483/kv/" + urllib.parse.quote(it["key"], safe="") + q,
        data=json.dumps(body).encode(), method="PUT", headers={"Content-Type": "application/vnd.microsoft.appconfig.kv+json"})
    r = json.load(urllib.request.urlopen(req)); print(r["key"], r["label"], r["value"])
