namespace Bookstore.Planning;

public sealed record Sprint(int Number, int[] DonePoints)
{
    public int Velocity => DonePoints.Sum();
    public int Throughput => DonePoints.Length;
}

public static class SprintHistory
{
    // Bookstore ekibi, son 10 sprint (2 hafta): Done'a giren her story'nin point'i
    public static readonly Sprint[] Last10 =
    [
        new(14, [5, 3, 8, 2, 3]),
        new(15, [3, 5, 2, 5, 2]),
        new(16, [8, 3, 5, 3, 2, 3]),
        new(17, [5, 5, 3, 3, 2, 1]),
        new(18, [8, 3, 3]),
        new(19, [5, 3, 3, 5, 2, 3, 1]),
        new(20, [3, 5, 8, 2, 2]),
        new(21, [5, 3, 5, 2, 3, 5]),
        new(22, [8, 5, 3, 2]),
        new(23, [3, 5, 3, 2, 5, 3, 1]),
    ];

    // Kurumsal Sipariş Release 1: planning poker sonrası 30 story
    public static readonly int[] Release1 =
    [
        1, 2, 2, 2, 2, 2, 3, 3, 3, 3,
        3, 3, 3, 3, 5, 5, 5, 5, 5, 5,
        5, 5, 5, 5, 5, 8, 8, 8, 8, 8,
    ];
}
