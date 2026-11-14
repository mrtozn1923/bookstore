using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Bookstore.Tools;

public static class MermaidErWriter
{
    public static string Write(IModel model)
    {
        var sb = new StringBuilder("erDiagram\n");
        var tables = model.GetRelationalModel().Tables.OrderBy(t => t.Name).ToList();

        foreach (var table in tables)
        {
            var pk = table.PrimaryKey!.Columns;
            sb.AppendLine($"    {table.Name} {{");
            foreach (var column in table.Columns)
            {
                List<string> keys = [];
                if (pk.Contains(column)) keys.Add("PK");
                if (table.ForeignKeyConstraints.Any(fk => fk.Columns.Contains(column))) keys.Add("FK");
                if (table.Indexes.Any(i => i.IsUnique && i.Columns.Contains(column))) keys.Add("UK");
                var type = column.StoreType.Replace(" ", "") + (column.IsNullable ? "?" : "");
                List<string> parts = [type, column.Name];
                if (keys.Count > 0) parts.Add(string.Join(", ", keys));
                if (column.ComputedColumnSql is { } sql) parts.Add($"\"computed: {sql}\"");
                sb.AppendLine("        " + string.Join(" ", parts));
            }
            sb.AppendLine("    }");
        }

        foreach (var table in tables)
        {
            var pk = table.PrimaryKey!.Columns;
            foreach (var fk in table.ForeignKeyConstraints.OrderBy(f => f.Name))
            {
                // Principal tarafı: FK nullable ise 0..1, değilse tam 1
                var left = fk.Columns.Any(c => c.IsNullable) ? "|o" : "||";
                // Dependent tarafı: FK unique ise en fazla 1, değilse çok
                var unique = fk.Columns.SequenceEqual(pk)
                    || table.Indexes.Any(i => i.IsUnique && i.Columns.SequenceEqual(fk.Columns));
                var right = unique ? "o|" : "o{";
                // FK, PK'nin parçasıysa identifying (düz çizgi), değilse non-identifying (kesikli)
                var line = fk.Columns.All(pk.Contains) ? "--" : "..";
                var rule = fk.OnDeleteAction == ReferentialAction.Cascade ? "cascade" : "restrict";
                var by = string.Join(",", fk.Columns.Select(c => c.Name));
                sb.AppendLine($"    {fk.PrincipalTable.Name} {left}{line}{right} {table.Name} : \"{by} ({rule})\"");
            }
        }
        return sb.ToString();
    }
}
