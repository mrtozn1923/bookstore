using System.Net.Http.Json;
using Bookstore.Institutional;

namespace Bookstore.Api.Tests;

public sealed class InstitutionalTests(BookstoreApiFactory factory)
{
    private readonly HttpClient client = factory.CreateClient();

    [Fact]
    public async Task Seeded_institutions_come_with_their_unit_hierarchy()
    {
        List<InstitutionView>? institutions = await client.GetFromJsonAsync<List<InstitutionView>>(
            "/api/institutions", TestContext.Current.CancellationToken);

        Assert.NotNull(institutions);
        Assert.Equal(["Central City Library", "Riverside High School"], institutions.Select(i => i.Name));

        InstitutionView school = institutions.Single(i => i.Code == "RHS");
        Assert.Equal("School", school.Kind);
        InstitutionUnitView science = school.Units.Single(u => u.Name == "Science Department");
        Assert.Equal("North Campus", school.Units.Single(u => u.Id == science.ParentUnitId).Name);
    }
}
