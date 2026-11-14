import dkim, spf
from authheaders import authenticate_message

def message(sender: str, body: str) -> bytes:
    return (f"From: Bookstore <{sender}>\r\n"
            "To: Ada <ada@example.com>\r\n"
            "Subject: Order BK-1042 confirmed\r\n"
            "Date: Sat, 14 Nov 2026 10:15:00 +0000\r\n"
            "Message-ID: <bk-1042@mail.bookstore.example>\r\n"
            "\r\n" + body).encode()

def sign(msg: bytes, selector: str, domain: str, key_file: str) -> bytes:
    key = open(key_file, "rb").read()
    header = dkim.sign(msg, selector.encode(), domain.encode(), key,
                       include_headers=[b"From", b"To", b"Subject", b"Date", b"Message-ID"])
    return header + msg

print("SPF")
for ip, sender, helo in [("198.51.100.25", "bounces@mail.bookstore.example", "mta1.txprovider.example"),
                         ("203.0.113.10", "bounces@mail.bookstore.example", "web1.bookstore.example"),
                         ("203.0.113.130", "ayse@bookstore.example", "mx1.corpmail.example")]:
    result, explanation = spf.check2(i=ip, s=sender, h=helo)
    print(f"  {ip:<15} {sender:<32} -> {result}")

body = "Your order BK-1042 (Dune, Emma) is confirmed. Total: 640.00 TRY\r\n"
signed = sign(message("orders@bookstore.example", body), "s1", "mail.bookstore.example", "/work/s1.private.pem")
print(b"\r\n".join(signed.split(b"\r\n")[:3]).decode())
print("DKIM verify (original):", dkim.verify(signed))
tampered = signed.replace(b"640.00", b"6.40")
print("DKIM verify (body changed):", dkim.verify(tampered))

def results(msg: bytes, mail_from: str) -> str:
    return authenticate_message(msg, "mx.example.com", ip="198.51.100.25", mail_from=mail_from,
                                helo="mta1.txprovider.example", spf=True, dkim=True, dmarc=True)

print("Own domain, aligned:")
print("  " + results(signed, "bounces@mail.bookstore.example"))
provider_signed = sign(message("orders@bookstore.example", body), "tx1", "txprovider.example", "/work/tx1.private.pem")
print("Provider defaults, not aligned:")
print("  " + results(provider_signed, "bounces@txprovider.example"))
