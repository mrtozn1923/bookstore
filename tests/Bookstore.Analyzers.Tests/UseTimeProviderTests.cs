using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace Bookstore.Analyzers.Tests;

public sealed class UseTimeProviderTests
{
    [Fact]
    public async Task UtcNow_is_replaced_with_the_injected_clock()
    {
        const string before = """
            using System;

            public sealed class CartCleanup(TimeProvider clock)
            {
                public DateTime Cutoff() => {|BS0001:DateTime.UtcNow|}.AddDays(-30);
            }
            """;
        const string after = """
            using System;

            public sealed class CartCleanup(TimeProvider clock)
            {
                public DateTime Cutoff() => clock.GetUtcNow().UtcDateTime.AddDays(-30);
            }
            """;

        await VerifyFixAsync(before, after);
    }

    [Fact]
    public async Task DateTimeOffset_Now_becomes_GetLocalNow()
    {
        const string before = """
            using System;

            public sealed class Receipt
            {
                private readonly TimeProvider time = TimeProvider.System;

                public DateTimeOffset PrintedAt() => {|BS0001:DateTimeOffset.Now|};
            }
            """;
        const string after = """
            using System;

            public sealed class Receipt
            {
                private readonly TimeProvider time = TimeProvider.System;

                public DateTimeOffset PrintedAt() => time.GetLocalNow();
            }
            """;

        await VerifyFixAsync(before, after);
    }

    [Fact]
    public async Task Without_a_TimeProvider_in_scope_there_is_a_diagnostic_but_no_fix()
    {
        const string source = """
            using System;

            public static class Clock
            {
                public static DateTime Today() => {|BS0001:DateTime.Today|};
            }
            """;

        await VerifyFixAsync(source, source);
    }

    [Fact]
    public async Task Using_static_is_reported_too()
    {
        const string source = """
            using System;
            using static System.DateTime;

            public static class Clock
            {
                public static DateTime Now() => {|BS0001:UtcNow|};
            }
            """;

        await VerifyFixAsync(source, source);
    }

    [Fact]
    public async Task Other_DateTime_members_are_allowed()
    {
        const string source = """
            using System;

            public static class Dates
            {
                public static DateTime AsUtc(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);
                public static DateTime Epoch => DateTime.UnixEpoch;
            }
            """;

        await VerifyFixAsync(source, source);
    }

    private static Task VerifyFixAsync(string source, string fixedSource)
    {
        CSharpCodeFixTest<UseTimeProviderAnalyzer, UseTimeProviderCodeFix, DefaultVerifier> test = new()
        {
            TestCode = source,
            FixedCode = fixedSource,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net100,
        };
        return test.RunAsync(TestContext.Current.CancellationToken);
    }
}
