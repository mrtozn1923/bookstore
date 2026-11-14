using Bookstore.Crypto.Fields;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Bookstore.Customers;

// Associated data = column name: binds the ciphertext to the column, not to the row
public sealed class EncryptedStringConverter(FieldEncryptor encryptor, string column)
    : ValueConverter<string, string>(
        plaintext => encryptor.EncryptString(plaintext, column),
        payload => encryptor.DecryptString(payload, column));
