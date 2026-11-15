namespace Bookstore.Admin;

// The contract that scripts and CI pipelines rely on; the root command description repeats it.
public static class ExitCodes
{
    public const int Success = 0;
    public const int UsageError = 1;
    public const int InvalidData = 2;
    public const int Canceled = 130;
}
