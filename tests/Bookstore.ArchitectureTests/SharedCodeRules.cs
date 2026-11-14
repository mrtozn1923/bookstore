namespace Bookstore.ArchitectureTests;

public sealed class SharedCodeRules
{
    // The only shared code: contracts and ServiceDefaults. No "Common" or "Shared" business library.
    private static readonly string[] AllowedBuildingBlocks = ["Bookstore.Contracts", "Bookstore.ServiceDefaults"];

    // A contracts project is a description of messages, not a place for logic or infrastructure.
    private static readonly string[] AllowedContractPackages = [];

    public static TheoryData<string> ServiceProjectNames() => [.. RepositoryLayout.ServiceProjects().Select(p => p.Name)];

    [Theory]
    [MemberData(nameof(ServiceProjectNames))]
    public void A_service_references_only_its_own_projects_building_blocks_and_contracts(string projectName)
    {
        ProjectFile project = RepositoryLayout.ServiceProjects().Single(p => p.Name == projectName);
        string buildingBlocks = RepositoryLayout.Src("BuildingBlocks") + Path.DirectorySeparatorChar;

        string[] violations =
        [
            .. project.ProjectReferences.Where(reference =>
                !reference.StartsWith(project.Folder + Path.DirectorySeparatorChar, StringComparison.Ordinal) &&
                !reference.StartsWith(buildingBlocks, StringComparison.Ordinal) &&
                !Path.GetFileNameWithoutExtension(reference).EndsWith(".Contracts", StringComparison.Ordinal))
            .Select(Path.GetFileNameWithoutExtension)!
        ];

        Assert.True(violations.Length == 0,
            $"{project.Name} references another service's implementation: {string.Join(", ", violations)}");
    }

    [Fact]
    public void Building_blocks_contain_only_contracts_and_service_defaults()
    {
        string[] unexpected = [.. RepositoryLayout.BuildingBlocks().Select(p => p.Name).Except(AllowedBuildingBlocks)];

        Assert.True(unexpected.Length == 0, $"Unexpected shared projects: {string.Join(", ", unexpected)}");
    }

    [Fact]
    public void Contract_projects_have_no_logic_dependencies()
    {
        ProjectFile[] contracts =
        [
            .. RepositoryLayout.BuildingBlocks().Concat(RepositoryLayout.ServiceProjects())
                .Where(p => p.Name.EndsWith(".Contracts", StringComparison.Ordinal))
        ];

        foreach (ProjectFile contract in contracts)
        {
            Assert.Empty(contract.ProjectReferences);
            Assert.All(contract.PackageReferences, package => Assert.Contains(package, AllowedContractPackages));
        }
    }
}
