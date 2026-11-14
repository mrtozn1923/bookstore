using System.Collections;
using System.Reflection;
using System.Text;
using Bookstore.Institutional.Domain;

namespace Bookstore.Institutional.Diagrams;

public sealed class ObjectDiagram
{
    private readonly Dictionary<object, string> names = new(ReferenceEqualityComparer.Instance);
    private readonly StringBuilder objects = new();
    private readonly StringBuilder links = new();
    private readonly HashSet<(string, string)> linked = [];

    public ObjectDiagram Add(object instance, string name)
    {
        names[instance] = name;
        return this;
    }

    public string Render()
    {
        foreach ((object instance, string name) in names)
        {
            objects.AppendLine($"object \"{name} : {instance.GetType().Name}\" as {name} {{");
            foreach (PropertyInfo p in instance.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                object? value = p.GetValue(instance);
                if (value is null || names.ContainsKey(value))
                {
                    if (value is not null) Link(name, names[value], p.Name);
                    continue;
                }
                if (value is IEnumerable items and not string)
                {
                    foreach (object item in items)
                        if (names.TryGetValue(item, out string? target)) Link(name, target, p.Name);
                    continue;
                }
                if (p.Name.EndsWith("Id")) continue;
                objects.AppendLine($"  {p.Name} = {Format(value)}");
            }
            objects.AppendLine("}");
        }
        return $"@startuml\n{objects}{links}@enduml";
    }

    private void Link(string from, string to, string role)
    {
        // Çift yönlü association'ı tek link olarak çiz
        if (linked.Contains((to, from))) return;
        linked.Add((from, to));
        links.AppendLine($"{from} -- {to} : {role}");
    }

    private static string Format(object value) => value switch
    {
        Money m => $"{m.Amount} {m.Currency}",
        DateTimeOffset d => d.ToString("yyyy-MM-dd"),
        DateOnly d => d.ToString("yyyy-MM-dd"),
        string s => $"\"{s}\"",
        _ => value.ToString() ?? ""
    };
}
