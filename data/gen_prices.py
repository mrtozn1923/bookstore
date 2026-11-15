"""Generate the publisher price list used in post 23 (deterministic)."""
import sys
out = sys.argv[1]
books = [l.split("\t") for l in open(sys.argv[2], encoding="utf-8").read().split("\n") if "\t" in l]
titles = ["Suç ve Ceza", "Şeker Portakalı", "Küçük Prens", "Savaş ve Barış, Cilt 1", "Yüzyıllık Yalnızlık",
          "Gurur ve Önyargı", "Dönüşüm", "Çalıkuşu", "İnce Memed", "Uğultulu Tepeler"]
rows = []
for n in range(1, 200001):
    if n <= len(books):
        isbn, price = books[n - 1][0], float(books[n - 1][1])
        price = round(price * 1.08, 2)
    else:
        isbn = "979" + str(n).zfill(10)
        price = 5 + (n * 7919 % 4500) / 100
    title = f"{titles[n % len(titles)]} #{n}"
    p = f"{price:.2f}"
    if n % 20000 == 7:
        p = '"' + p.replace(".", ",") + '"'
    if n % 50000 == 11:
        isbn = ""
    rows.append(f'{isbn},"{title}",{p},TRY,2026-12-01')
hdr = "isbn,title,list_price,currency,valid_from"
open(f"{out}/prices.csv", "w", encoding="utf-8", newline="\n").write(hdr + "\n" + "\n".join(rows) + "\n")
open(f"{out}/prices_excel.csv", "w", encoding="utf-8-sig", newline="\r\n").write(hdr + "\n" + "\n".join(rows) + "\n")
print(len(rows))
