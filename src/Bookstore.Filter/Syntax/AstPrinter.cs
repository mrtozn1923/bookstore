using System.Globalization;
using System.Text;

namespace Bookstore.Filter.Syntax;

public static class AstPrinter
{
    public static string Print(Expr root)
    {
        StringBuilder sb = new();
        Write(sb, root, "", "", "");
        return sb.ToString();
    }

    private static void Write(StringBuilder sb, Expr node, string label, string indent, string childIndent)
    {
        sb.Append(indent).Append(label).AppendLine(Describe(node));
        Expr[] children = node switch
        {
            BinaryExpr b => [b.Left, b.Right],
            ComparisonExpr c => [c.Left, c.Right],
            NotExpr n => [n.Operand],
            _ => [],
        };
        for (int i = 0; i < children.Length; i++)
        {
            bool last = i == children.Length - 1;
            Write(sb, children[i], last ? "└─ " : "├─ ", childIndent, childIndent + (last ? "   " : "│  "));
        }
    }

    private static string Describe(Expr node) => node switch
    {
        BinaryExpr b => $"BinaryExpr {b.Operator} @{b.Position}",
        ComparisonExpr c => $"ComparisonExpr {Operators.Symbol(c.Operator)} @{c.Position}",
        NotExpr n => $"NotExpr @{n.Position}",
        Identifier id => $"Identifier {id.Name} @{id.Position}",
        Literal { Value: string s } l => $"Literal \"{s}\" @{l.Position}",
        Literal { Value: bool b } l => $"Literal {(b ? "true" : "false")} @{l.Position}",
        Literal l => $"Literal {Convert.ToString(l.Value, CultureInfo.InvariantCulture)} @{l.Position}",
        _ => $"ErrorExpr @{node.Position}",
    };
}
