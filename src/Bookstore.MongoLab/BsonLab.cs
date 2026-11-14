using System.Text;
using System.Text.Json;
using MongoDB.Bson;

namespace Bookstore.MongoLab;

public static class BsonLab
{
    public static void Run()
    {
        // The same book: JSON text and BSON bytes
        BsonDocument book = new()
        {
            { "_id", 26 },
            { "title", "Dune" },
            { "listPrice", new Decimal128(390.50m) },
            { "listedOn", new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc) },
        };

        byte[] bson = book.ToBson();
        string json = JsonSerializer.Serialize(new
        {
            _id = 26,
            title = "Dune",
            listPrice = 390.50m,
            listedOn = "2024-06-01T00:00:00Z",
        });

        Console.WriteLine($"JSON : {Encoding.UTF8.GetByteCount(json)} bytes  {json}");
        Console.WriteLine($"BSON : {bson.Length} bytes");
        Console.WriteLine();
        Dump(bson);
    }

    public static void ObjectIds()
    {
        ObjectId first = ObjectId.GenerateNewId();
        ObjectId second = ObjectId.GenerateNewId();

        foreach (ObjectId id in new[] { first, second })
        {
            string hex = id.ToString();
            Console.WriteLine($"{hex}  time={hex[..8]} random={hex[8..18]} counter={hex[18..]}");
        }

        Console.WriteLine($"CreationTime (UTC): {first.CreationTime:yyyy-MM-ddTHH:mm:ssZ}");
        Console.WriteLine($"first < second    : {first < second}");
    }

    // Each element: 1 type byte + name (ending in 0x00) + value
    private static void Dump(byte[] bytes)
    {
        int length = BitConverter.ToInt32(bytes, 0);
        Console.WriteLine($"{"length = " + length,-24} {Hex(bytes, 0, 4)}");

        int position = 4;
        while (bytes[position] != 0x00)
        {
            byte type = bytes[position];
            int nameEnd = Array.IndexOf(bytes, (byte)0x00, position + 1);
            string name = Encoding.UTF8.GetString(bytes, position + 1, nameEnd - position - 1);
            int valueStart = nameEnd + 1;
            int valueLength = type switch
            {
                0x10 => 4,                                                  // int32
                0x02 => 4 + BitConverter.ToInt32(bytes, valueStart),        // string
                0x13 => 16,                                                 // decimal128
                0x09 => 8,                                                  // UTC datetime
                _ => throw new NotSupportedException($"type 0x{type:x2}"),
            };

            int end = valueStart + valueLength;
            Console.WriteLine($"{name + " (" + TypeName(type) + ")",-24} {Hex(bytes, position, end - position)}");
            position = end;
        }

        Console.WriteLine($"{"end of document",-24} {Hex(bytes, position, 1)}");
    }

    private static string TypeName(byte type) => type switch
    {
        0x10 => "int32",
        0x02 => "string",
        0x13 => "decimal128",
        0x09 => "date",
        _ => "?",
    };

    private static string Hex(byte[] bytes, int start, int count)
    {
        string hex = Convert.ToHexString(bytes, start, count).ToLowerInvariant();
        return string.Join(' ', Enumerable.Range(0, hex.Length / 2).Select(i => hex.Substring(i * 2, 2)));
    }
}
