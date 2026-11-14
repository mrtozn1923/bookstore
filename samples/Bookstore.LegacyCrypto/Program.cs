using System.Security.Cryptography;
using System.Text;
using Bookstore.Crypto.Fields;
using Bookstore.Crypto.Legacy;
using Bookstore.LegacyCrypto;

const string Password = "dev-only-legacy-export-password";
string csv = "customerId;name;phone\n1001;Ada Lovelace;+90 555 010 1001\n1002;Alan Turing;+90 555 010 1002";

// 1) An old export file, produced the old way
byte[] legacyFile = LegacyExportTool.Export(Password, csv);
Console.WriteLine($"legacy export: {legacyFile.Length} bytes, checksum {LegacyExportTool.Checksum(legacyFile)[..16]}...");

// 2) Read it with today's APIs: same algorithm, same parameters, no obsolete type
string read = Encoding.UTF8.GetString(LegacyExportCipher.Decrypt(Password, legacyFile));
Console.WriteLine($"read with Rfc2898DeriveBytes.Pbkdf2 + Aes.DecryptCbc: {read.Split('\n').Length - 1} customers, first: {read.Split('\n')[1]}");
Console.WriteLine($"SHA256Managed == SHA256.HashData: {LegacyExportTool.Checksum(legacyFile) == Convert.ToHexString(SHA256.HashData(legacyFile))}");

// 3) Migrate: re-encrypt each phone number in the current versioned format (AES-GCM, keyId)
FieldEncryptor encryptor = new(new FieldKeyRing(1, new Dictionary<byte, byte[]> { [1] = Encoding.ASCII.GetBytes("dev-only-bookstore-field-key-v01") }));
foreach (string line in read.Split('\n').Skip(1))
{
    string[] parts = line.Split(';');
    string stored = encryptor.EncryptString(parts[2], "Customer.Phone");
    Console.WriteLine($"customer {parts[0]}: v{Convert.FromBase64String(stored)[0]} keyId {FieldEncryptor.KeyIdOf(stored)}, {stored[..20]}...");
}
