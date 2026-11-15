using System.IO.Hashing;
using System.Numerics;
using System.Text;

namespace Algorithms.Probabilistic;

// 2^p registers; each keeps the longest run of leading zeros seen in its share of the hashes
public sealed class HyperLogLog
{
    private readonly byte[] _registers;
    private readonly int _p;

    public HyperLogLog(int precision = 14)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(precision, 4);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(precision, 18);
        _p = precision;
        _registers = new byte[1 << precision];
    }

    public int RegisterCount => _registers.Length;

    public double StandardError => 1.04 / Math.Sqrt(_registers.Length);

    public void Add(string item)
    {
        ulong hash = XxHash3.HashToUInt64(Encoding.UTF8.GetBytes(item));
        int index = (int)(hash >> (64 - _p));                          // first p bits pick the register
        ulong rest = (hash << _p) | (1UL << (_p - 1));                 // guard bit keeps rho bounded
        byte rho = (byte)(BitOperations.LeadingZeroCount(rest) + 1);   // position of the first 1 bit
        if (rho > _registers[index]) _registers[index] = rho;
    }

    public long Count()
    {
        int m = _registers.Length;
        double alpha = 0.7213 / (1 + 1.079 / m);
        double sum = 0;
        int zeros = 0;
        foreach (byte r in _registers)
        {
            sum += Math.Pow(2, -r);
            if (r == 0) zeros++;
        }

        double estimate = alpha * m * m / sum;
        if (estimate <= 2.5 * m && zeros > 0) estimate = m * Math.Log((double)m / zeros); // small range: linear counting
        return (long)Math.Round(estimate);
    }

    // The union of two sets: the larger value of each register
    public void Merge(HyperLogLog other)
    {
        if (other._p != _p) throw new ArgumentException("Precision must match.", nameof(other));
        for (int i = 0; i < _registers.Length; i++)
            _registers[i] = Math.Max(_registers[i], other._registers[i]);
    }
}
