namespace Bookstore.Planning;

public static class PointsVsCount
{
    public static void Run()
    {
        Sprint[] sprints = SprintHistory.Last10;
        double[] velocity = [.. sprints.Select(s => (double)s.Velocity)];
        double[] throughput = [.. sprints.Select(s => (double)s.Throughput)];

        Console.WriteLine("Sprint  points  stories");
        foreach (Sprint s in sprints)
            Console.WriteLine($"  {s.Number,4}  {s.Velocity,6}  {s.Throughput,7}");
        Console.WriteLine($"  mean  {velocity.Average(),6:F1}  {throughput.Average(),7:F1}");
        Console.WriteLine($"Pearson r (points vs stories) = {Correlation(velocity, throughput):F2}");

        // Aynı soru iki yoldan: Release 1 kaç sprint sürer?
        int[] backlog = SprintHistory.Release1;
        Console.WriteLine();
        Console.WriteLine($"Release 1: {backlog.Length} stories, {backlog.Sum()} points");
        Console.WriteLine($"  by points : {backlog.Sum() / velocity.Average():F1} sprints");
        Console.WriteLine($"  by count  : {backlog.Length / throughput.Average():F1} sprints");

        // Sayma yöntemi, story boyut dağılımı geçmişe benziyorsa işe yarar
        int[] done = [.. sprints.SelectMany(s => s.DonePoints)];
        Console.WriteLine();
        Console.WriteLine($"Average story size: history {done.Average():F2}, Release 1 {backlog.Average():F2}");
        foreach (int size in new[] { 1, 2, 3, 5, 8 })
            Console.WriteLine($"  {size} pt: history {Share(done, size),3:F0}%   Release 1 {Share(backlog, size),3:F0}%");
    }

    static double Share(int[] stories, int size) => stories.Count(p => p == size) * 100.0 / stories.Length;

    static double Correlation(double[] x, double[] y)
    {
        double mx = x.Average(), my = y.Average();
        double cov = x.Zip(y, (a, b) => (a - mx) * (b - my)).Sum();
        double sx = Math.Sqrt(x.Sum(a => (a - mx) * (a - mx)));
        double sy = Math.Sqrt(y.Sum(b => (b - my) * (b - my)));
        return cov / (sx * sy);
    }
}
