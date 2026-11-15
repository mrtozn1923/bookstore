using Bookstore.Privacy.Transfers;

namespace Bookstore.PrivacyLab;

public static class TransfersCommand
{
    public static void Run()
    {
        foreach (SubProcessor s in SubProcessors.All)
        {
            string where = s.Abroad ? "abroad" : "domestic";
            Console.WriteLine($"{s.Key,-17} {where,-9} {s.Mechanism,-17} {string.Join(", ", s.DataCategories)}");
        }
    }
}
