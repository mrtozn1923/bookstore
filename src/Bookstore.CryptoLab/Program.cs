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
    default: Console.Error.WriteLine($"Unknown command: {command}"); return 1;
}
return 0;
