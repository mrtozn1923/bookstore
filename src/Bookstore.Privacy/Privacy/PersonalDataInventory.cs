using System.Reflection;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Compliance.Classification;

namespace Bookstore.Privacy;

public sealed record InventoryColumn(string Table, string Column, string Classification, string InformationType);

public sealed record InventoryEntry(ProcessingActivity Activity, string Table, IReadOnlyList<InventoryColumn> Columns);

public static class PersonalDataInventory
{
    // Reads the EF Core model and the attributes on the CLR properties.
    public static IReadOnlyList<InventoryEntry> Build(DbContext db)
    {
        List<InventoryEntry> entries = [];
        foreach (IEntityType entity in db.Model.GetEntityTypes().OrderBy(e => e.GetTableName()))
        {
            string table = entity.GetTableName()!;
            string category = entity.ClrType.GetCustomAttribute<DataCategoryAttribute>()?.Category
                ?? throw new InvalidOperationException($"{entity.ClrType.Name} has no [DataCategory].");
            ProcessingActivity activity = ProcessingActivities.All.Single(a => a.Category == category);

            List<InventoryColumn> columns = [];
            foreach (IProperty property in entity.GetProperties())
            {
                (string classification, string infoType) = Classify(property);
                if (classification != "None")
                {
                    columns.Add(new InventoryColumn(table, property.GetColumnName(), classification, infoType));
                }
            }
            entries.Add(new InventoryEntry(activity, table, columns));
        }
        return entries;
    }

    public static IEnumerable<string> UnclassifiedProperties(DbContext db) =>
        from entity in db.Model.GetEntityTypes()
        from property in entity.GetProperties()
        where property.PropertyInfo is { } info && info.GetCustomAttribute<DataClassificationAttribute>() is null
        select $"{entity.ClrType.Name}.{property.Name}";

    public static string ToSensitivityClassificationSql(IEnumerable<InventoryEntry> entries)
    {
        var sql = new StringBuilder();
        foreach (InventoryColumn c in entries.SelectMany(e => e.Columns))
        {
            (string label, string rank) = c.Classification == nameof(BookstoreTaxonomy.SensitiveData)
                ? ("Highly Confidential", "HIGH")
                : ("Confidential", "MEDIUM");
            sql.AppendLine($"ADD SENSITIVITY CLASSIFICATION TO dbo.{c.Table}.{c.Column}");
            sql.AppendLine($"WITH (LABEL = '{label}', INFORMATION_TYPE = '{c.InformationType}', RANK = {rank});");
        }
        return sql.ToString();
    }

    private static (string Classification, string InformationType) Classify(IProperty property)
    {
        DataClassificationAttribute? attribute = property.PropertyInfo?.GetCustomAttribute<DataClassificationAttribute>();
        return attribute switch
        {
            PersonalDataAttribute p => (nameof(BookstoreTaxonomy.PersonalData), p.InformationType),
            SensitiveDataAttribute s => (nameof(BookstoreTaxonomy.SensitiveData), s.InformationType),
            _ => ("None", ""),
        };
    }
}
