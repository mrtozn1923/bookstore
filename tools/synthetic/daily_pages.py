"""Writes data/daily-pages.csv: SYNTHETIC daily Search Console-like rows for the post 07 examples.
Known truth built in: from 2026-10-14 the three retitled posts get +15% CTR, ramping in over 7 days.
Everything else is season, weekday, a holiday (29 October) and random variation.
Usage (from the repository root): python3 tools/synthetic/daily_pages.py"""
import datetime

seed = [20261115]


def rnd():
    seed[0] = (seed[0] * 16807) % 2147483647
    return seed[0] / 2147483647


pages = {  # page: (daily impressions, ctr)
    "/blog/ef-core-migration-rehberi": (420, 0.050),
    "/blog/dependency-injection-nedir": (300, 0.030),
    "/blog/async-await-nasil-calisir": (220, 0.045),
    "/blog/cs8618-nullable-uyarisi": (260, 0.090),
    "/blog/linq-groupby": (150, 0.040),
    "/blog/ef-core-vs-dapper": (180, 0.060),
    "/blog/dapper-ile-toplu-insert": (90, 0.055),
    "/blog/minimal-api-validation": (160, 0.020),
    "/blog/record-struct": (70, 0.050),
    "/blog/span-t-nedir": (60, 0.060),
}
retitled = {"/blog/ef-core-migration-rehberi", "/blog/dependency-injection-nedir", "/blog/async-await-nasil-calisir"}
change = datetime.date(2026, 10, 14)
season = {6: 0.9, 7: 0.75, 8: 0.7, 9: 1.0, 10: 1.05, 11: 1.05}

rows = []
d = datetime.date(2026, 6, 1)
while d <= datetime.date(2026, 11, 12):
    weekday = 0.55 if d.weekday() >= 5 else 1.0
    holiday = 0.6 if d == datetime.date(2026, 10, 29) else 1.0
    for page, (impressions, ctr) in pages.items():
        i = impressions * season[d.month] * weekday * holiday * (0.85 + 0.3 * rnd())
        c = ctr
        if page in retitled and d > change:
            c = ctr * (1 + 0.15 * min(1.0, (d - change).days / 7))
        clicks = i * c * (0.85 + 0.3 * rnd())
        rows.append((d.isoformat(), page, round(clicks), round(i)))
    d += datetime.timedelta(days=1)

with open("data/daily-pages.csv", "w") as f:
    f.write("date,page,clicks,impressions\n")
    for r in rows:
        f.write(",".join(map(str, r)) + "\n")
print(len(rows), "rows")
