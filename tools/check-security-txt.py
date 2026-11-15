"""Validate a security.txt file offline with the sectxt package. Usage: pip install sectxt==0.9.8; python tools/check-security-txt.py src/Bookstore.Api/security.txt"""
import sys, warnings
warnings.filterwarnings("ignore")
from sectxt import Parser

p = Parser(open(sys.argv[1], "rb").read(), urls="https://bookstore.example.com/.well-known/security.txt")
print("valid:", p.is_valid())
for kind in ("errors", "recommendations", "notifications"):
    for m in getattr(p, kind):
        print(f"{kind[:-1]}: {m['code']}: {m['message']}")
