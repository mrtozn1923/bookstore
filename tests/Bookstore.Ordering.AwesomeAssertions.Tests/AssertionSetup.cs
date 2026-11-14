using AwesomeAssertions.Extensibility;

[assembly: AssertionEngineInitializer(typeof(Bookstore.Ordering.AwesomeAssertions.Tests.AssertionSetup), nameof(Bookstore.Ordering.AwesomeAssertions.Tests.AssertionSetup.Initialize))]

namespace Bookstore.Ordering.AwesomeAssertions.Tests;

public static class AssertionSetup
{
    // Entities override Equals by Id; compare them member by member in every BeEquivalentTo
    public static void Initialize() =>
        AssertionConfiguration.Current.Equivalency.Modify(options => options.ComparingByMembers<Entity>());
}
