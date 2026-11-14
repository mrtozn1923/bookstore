import re

m = re.match(r"(?P<year>\d{4})-(\d{2})", "2026-11")
print(m.group(1), m.group(2), m.group("year"))

try:
    re.compile(r"(?<year>\d{4})")
except re.error as e:
    print("re.error:", e)
