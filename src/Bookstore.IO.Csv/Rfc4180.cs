using System.Text;

namespace Bookstore.IO.Csv;

// A minimal RFC 4180 record reader: quoted fields may contain the separator, "" and line breaks.
public static class Rfc4180
{
    public static List<string>? ReadRecord(TextReader reader, char separator = ',')
    {
        int c = reader.Read();
        if (c == -1) return null;

        var fields = new List<string>();
        var field = new StringBuilder();
        bool inQuotes = false;
        while (true)
        {
            if (inQuotes)
            {
                if (c == -1) throw new FormatException("Unclosed quote at end of input.");
                if (c == '"')
                {
                    // "" tırnağın içinde tek bir çift tırnaktır; tek " alanı kapatır.
                    if (reader.Peek() == '"') { field.Append('"'); reader.Read(); }
                    else inQuotes = false;
                }
                else field.Append((char)c);
            }
            else if (c == '"' && field.Length == 0) inQuotes = true;
            else if (c == separator) { fields.Add(field.ToString()); field.Clear(); }
            else if (c is '\r' or '\n' or -1)
            {
                if (c == '\r' && reader.Peek() == '\n') reader.Read();
                fields.Add(field.ToString());
                return fields;
            }
            else field.Append((char)c);

            c = reader.Read();
        }
    }
}
