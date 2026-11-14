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
    default: Console.Error.WriteLine($"Unknown command: {command}"); return 1;
}
return 0;
