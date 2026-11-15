"""Fake bank FTPS server for local tests (explicit TLS, passive mode, one test user).
Usage, from the repository root:  pip install pyftpdlib pyopenssl  then  python tools/fake-bank-ftps.py
Files live in .local/bank-ftp/statements; the certificate to trust is written to .local/bank-ftp/cert.pem."""
import os

from OpenSSL import crypto
from pyftpdlib.authorizers import DummyAuthorizer
from pyftpdlib.handlers import TLS_FTPHandler
from pyftpdlib.servers import FTPServer

ROOT = ".local/bank-ftp"
os.makedirs(f"{ROOT}/files/statements/processed", exist_ok=True)

# A self-signed certificate for 127.0.0.1, created on first start.
if not os.path.exists(f"{ROOT}/cert.pem"):
    key = crypto.PKey()
    key.generate_key(crypto.TYPE_RSA, 2048)
    cert = crypto.X509()
    cert.get_subject().CN = "fake-bank.example.com"
    cert.set_serial_number(1)
    cert.gmtime_adj_notBefore(-300)
    cert.gmtime_adj_notAfter(30 * 24 * 3600)
    cert.set_issuer(cert.get_subject())
    cert.set_pubkey(key)
    cert.add_extensions([crypto.X509Extension(b"subjectAltName", False, b"IP:127.0.0.1")])
    cert.sign(key, "sha256")
    with open(f"{ROOT}/cert.pem", "wb") as f:
        f.write(crypto.dump_certificate(crypto.FILETYPE_PEM, cert))
    with open(f"{ROOT}/key.pem", "wb") as f:
        f.write(crypto.dump_privatekey(crypto.FILETYPE_PEM, key))

authorizer = DummyAuthorizer()
authorizer.add_user("bookstore-test", "example-password-change-me", f"{ROOT}/files", perm="elradfmw")

handler = TLS_FTPHandler
handler.certfile = f"{ROOT}/cert.pem"
handler.keyfile = f"{ROOT}/key.pem"
handler.tls_control_required = True
handler.tls_data_required = True
handler.authorizer = authorizer
handler.passive_ports = range(52430, 52440)

print("Fake bank FTPS: ftp://127.0.0.1:52421 (explicit TLS)")
FTPServer(("127.0.0.1", 52421), handler).serve_forever()
