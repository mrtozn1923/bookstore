using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Bookstore.Analyzers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UseTimeProviderCodeFix)), Shared]
public sealed class UseTimeProviderCodeFix : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(UseTimeProviderAnalyzer.DiagnosticId);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        SyntaxNode? root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        SemanticModel? model = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null || model is null)
        {
            return;
        }

        if (root.FindNode(context.Span, getInnermostNodeForTie: true) is not MemberAccessExpressionSyntax access
            || ClockCall(model, access) is not { } call)
        {
            return;
        }

        // Only offer a fix when a TimeProvider is already in scope (field, property, parameter or local).
        INamedTypeSymbol? timeProvider = model.Compilation.GetTypeByMetadataName("System.TimeProvider");
        string? clock = model.LookupSymbols(access.SpanStart)
            .Where(s => s is IFieldSymbol or IPropertySymbol or IParameterSymbol or ILocalSymbol)
            .Where(s => SymbolEqualityComparer.Default.Equals(TypeOf(s), timeProvider))
            .Select(s => s.Name)
            .OrderBy(n => n, StringComparer.Ordinal)
            .FirstOrDefault();
        if (clock is null)
        {
            return;
        }

        string replacement = $"{clock}.{call}";
        context.RegisterCodeFix(
            CodeAction.Create(
                title: $"Use '{replacement}'",
                createChangedDocument: _ => Task.FromResult(context.Document.WithSyntaxRoot(
                    root.ReplaceNode(access, SyntaxFactory.ParseExpression(replacement).WithTriviaFrom(access)))),
                equivalenceKey: nameof(UseTimeProviderCodeFix)),
            context.Diagnostics);
    }

    private static string? ClockCall(SemanticModel model, MemberAccessExpressionSyntax access) =>
        model.GetSymbolInfo(access).Symbol is IPropertySymbol property
            ? (property.ContainingType.Name, property.Name) switch
            {
                ("DateTime", "UtcNow") => "GetUtcNow().UtcDateTime",
                ("DateTime", "Now") => "GetLocalNow().DateTime",
                ("DateTime", "Today") => "GetLocalNow().Date",
                ("DateTimeOffset", "UtcNow") => "GetUtcNow()",
                ("DateTimeOffset", "Now") => "GetLocalNow()",
                _ => null,
            }
            : null;

    private static ITypeSymbol? TypeOf(ISymbol symbol) => symbol switch
    {
        IFieldSymbol field => field.Type,
        IPropertySymbol property => property.Type,
        IParameterSymbol parameter => parameter.Type,
        ILocalSymbol local => local.Type,
        _ => null,
    };
}
