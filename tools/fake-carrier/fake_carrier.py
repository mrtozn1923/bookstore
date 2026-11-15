"""A stand-in for the carrier's quote API, for local development only.

Listens on 127.0.0.1: HTTP on 52682 and HTTPS on 52683. On first start it creates a self-signed
certificate in .certs/ (not committed). Bookstore.Api trusts that file for this client only.
Run it with the Python that has mitmproxy installed (it needs the cryptography package):
    python tools/fake-carrier/fake_carrier.py
"""
import datetime
import ipaddress
import json
import ssl
import threading
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path

from cryptography import x509
from cryptography.hazmat.primitives import hashes, serialization
from cryptography.hazmat.primitives.asymmetric import ec
from cryptography.x509.oid import NameOID

HOST = "127.0.0.1"
HTTP_PORT = 52682
HTTPS_PORT = 52683
API_KEY = "fake-carrier-key-for-tests"
CERT_DIR = Path(__file__).parent / ".certs"
CERT_FILE = CERT_DIR / "carrier-cert.pem"
KEY_FILE = CERT_DIR / "carrier-key.pem"


def ensure_certificate() -> None:
    if CERT_FILE.exists() and KEY_FILE.exists():
        return
    CERT_DIR.mkdir(exist_ok=True)
    key = ec.generate_private_key(ec.SECP256R1())
    name = x509.Name([x509.NameAttribute(NameOID.COMMON_NAME, "fake carrier (local dev)")])
    now = datetime.datetime.now(datetime.timezone.utc)
    cert = (
        x509.CertificateBuilder()
        .subject_name(name)
        .issuer_name(name)
        .public_key(key.public_key())
        .serial_number(x509.random_serial_number())
        .not_valid_before(now - datetime.timedelta(minutes=5))
        .not_valid_after(now + datetime.timedelta(days=30))
        .add_extension(x509.BasicConstraints(ca=True, path_length=0), critical=True)
        .add_extension(
            x509.SubjectAlternativeName(
                [x509.DNSName("localhost"), x509.IPAddress(ipaddress.ip_address(HOST))]
            ),
            critical=False,
        )
        .sign(key, hashes.SHA256())
    )
    KEY_FILE.write_bytes(
        key.private_bytes(
            serialization.Encoding.PEM,
            serialization.PrivateFormat.PKCS8,
            serialization.NoEncryption(),
        )
    )
    CERT_FILE.write_bytes(cert.public_bytes(serialization.Encoding.PEM))


class QuoteHandler(BaseHTTPRequestHandler):
    quotes = 0
    lock = threading.Lock()

    def do_POST(self) -> None:
        if self.path != "/v1/quotes":
            self.send_json(404, {"error": "not_found"})
            return
        if self.headers.get("Authorization") != f"Bearer {API_KEY}":
            self.send_json(401, {"error": "invalid_api_key"})
            return
        body = json.loads(self.read_body())
        with QuoteHandler.lock:
            QuoteHandler.quotes += 1
            number = QuoteHandler.quotes
        price = 49.90 + 10 * (max(0, body["weightGrams"] - 1000) // 1000)
        self.send_json(
            200,
            {"quoteId": f"q-{number:04d}", "price": price, "currency": "TRY", "estimatedDays": 2},
        )

    def read_body(self) -> bytes:
        # HttpClient's JsonContent sends the body chunked (no Content-Length).
        if self.headers.get("Transfer-Encoding", "").lower() != "chunked":
            return self.rfile.read(int(self.headers.get("Content-Length", "0")))
        data = b""
        while (size := int(self.rfile.readline().strip(), 16)) > 0:
            data += self.rfile.read(size)
            self.rfile.readline()
        self.rfile.readline()
        return data

    def send_json(self, status: int, payload: dict) -> None:
        data = json.dumps(payload).encode()
        self.send_response(status)
        self.send_header("Content-Type", "application/json")
        self.send_header("Content-Length", str(len(data)))
        self.end_headers()
        self.wfile.write(data)


def serve(port: int, tls: ssl.SSLContext | None) -> None:
    server = ThreadingHTTPServer((HOST, port), QuoteHandler)
    if tls is not None:
        server.socket = tls.wrap_socket(server.socket, server_side=True)
    print(f"fake carrier listening on {'https' if tls else 'http'}://{HOST}:{port}", flush=True)
    server.serve_forever()


if __name__ == "__main__":
    ensure_certificate()
    context = ssl.SSLContext(ssl.PROTOCOL_TLS_SERVER)
    context.load_cert_chain(CERT_FILE, KEY_FILE)
    threading.Thread(target=serve, args=(HTTP_PORT, None), daemon=True).start()
    serve(HTTPS_PORT, context)
