using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Bookstore.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UseTimeProviderAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "BS0001";

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "Use TimeProvider instead of the system clock",
        messageFormat: "'{0}' reads the system clock; use an injected TimeProvider",
        category: "Reliability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Bookstore code gets the current time from TimeProvider so that tests can control the clock.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(start =>
        {
            ImmutableHashSet<IPropertySymbol> clockProperties = FindClockProperties(start.Compilation);
            if (clockProperties.IsEmpty)
            {
                return;
            }

            start.RegisterOperationAction(
                operation => AnalyzePropertyReference(operation, clockProperties),
                OperationKind.PropertyReference);
        });
    }

    private static ImmutableHashSet<IPropertySymbol> FindClockProperties(Compilation compilation)
    {
        ImmutableHashSet<IPropertySymbol>.Builder builder =
            ImmutableHashSet.CreateBuilder<IPropertySymbol>(SymbolEqualityComparer.Default);
        AddProperties(builder, compilation.GetSpecialType(SpecialType.System_DateTime), "Now", "UtcNow", "Today");
        AddProperties(builder, compilation.GetTypeByMetadataName("System.DateTimeOffset"), "Now", "UtcNow");
        return builder.ToImmutable();
    }

    private static void AddProperties(
        ImmutableHashSet<IPropertySymbol>.Builder builder, INamedTypeSymbol? type, params string[] names)
    {
        if (type is null)
        {
            return;
        }

        foreach (string name in names)
        {
            foreach (IPropertySymbol property in type.GetMembers(name).OfType<IPropertySymbol>())
            {
                builder.Add(property);
            }
        }
    }

    private static void AnalyzePropertyReference(
        OperationAnalysisContext context, ImmutableHashSet<IPropertySymbol> clockProperties)
    {
        IPropertyReferenceOperation reference = (IPropertyReferenceOperation)context.Operation;
        if (!clockProperties.Contains(reference.Property))
        {
            return;
        }

        string name = $"{reference.Property.ContainingType.Name}.{reference.Property.Name}";
        context.ReportDiagnostic(Diagnostic.Create(Rule, reference.Syntax.GetLocation(), name));
    }
}
