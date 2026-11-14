using Bookstore.CryptoLab;

string command = args.FirstOrDefault() ?? "capabilities";
switch (command)
{
    case "capabilities": Commands.Capabilities(); break;
    case "providers": Commands.Providers(); break;
    case "hash-api": Commands.HashApi(); break;
    case "zeroize": Commands.Zeroize(); break;
    case "catalog-checksum": await HashCommands.CatalogChecksumAsync(); break;
    case "encoding": HashCommands.EncodingPitfall(); break;
    case "webhook": HashCommands.WebhookDemo(); break;
    case "derive-keys": HashCommands.DeriveKeys(); break;
    case "kmac": HashCommands.Kmac(); break;
    case "modes": EncryptionCommands.Modes(); break;
    case "aead": EncryptionCommands.Aead(); break;
    case "chacha": EncryptionCommands.ChaCha(); break;
    case "envelope": EncryptionCommands.Envelope(); break;
    case "customers": await EncryptionCommands.CustomersAsync(); break;
    case "key-sizes": AsymmetricCommands.KeySizes(); break;
    case "rsa-limit": AsymmetricCommands.RsaLimit(); break;
    case "purchase-order": AsymmetricCommands.PurchaseOrderDemo(); break;
    case "rsa-certificate": AsymmetricCommands.RsaFromCertificate(); break;
    case "signatures": SignatureCommands.Formats(); break;
    case "public-key-sign": SignatureCommands.PublicKeyCannotSign(); break;
    case "license": SignatureCommands.LicenseDemo(); break;
    case "pki-init": PkiCommands.Init(); break;
    case "cert-info": PkiCommands.Info(); break;
    case "chain": PkiCommands.Chain(); break;
    case "pfx-export": PkiCommands.ExportPfx(); break;
    case "mtls-call": await PkiCommands.CallAsync(args); break;
    case "tls-handshake": await TlsCommands.HandshakeAsync(); break;
    case "tls-keylog": await TlsCommands.KeyLogAsync(); break;
    case "tls-server": await TlsCommands.ServeAsync(args); break;
    case "pqc-sizes": PqcCommands.Sizes(); break;
    case "pqc-envelope": PqcCommands.Envelope(); break;
    case "tls-groups": await PqcCommands.GroupsAsync(); break;
    default: Console.Error.WriteLine($"Unknown command: {command}"); return 1;
}
return 0;
