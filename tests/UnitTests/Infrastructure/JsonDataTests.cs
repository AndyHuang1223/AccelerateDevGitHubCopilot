using System.Text.Json;
using Library.Infrastructure.Data;
using Microsoft.Extensions.Configuration;

namespace Library.UnitTests.Infrastructure;

public sealed class JsonDataTests : IDisposable
{
    private readonly string _runtimeRoot;

    public JsonDataTests()
    {
        _runtimeRoot = Path.Combine(
            Path.GetTempPath(),
            $"library-json-data-tests-{Guid.NewGuid():N}");
    }

    [Fact]
    public async Task InitializeAsync_CreatesRuntimeDataWithTeachingFixtures()
    {
        var data = new JsonData(CreateConfiguration());

        await data.InitializeAsync();

        Assert.Equal(_runtimeRoot, data.RuntimeDataRootPath);
        Assert.Equal(50, data.Patrons!.Count);
        Assert.Equal(20, data.BookItems!.Count);
        Assert.Equal(19, data.Loans!.Count);

        var activeLoans = data.Loans.Where(loan => loan.ReturnDate == null).ToList();
        Assert.Equal(17, activeLoans.Count);
        Assert.Equal(activeLoans.Count, activeLoans.Select(loan => loan.BookItemId).Distinct().Count());

        Assert.Equal(0, GetPatronLoans(data, 1).Count(loan => loan.ReturnDate == null));
        Assert.Equal(4, GetPatronLoans(data, 2).Count(loan => loan.ReturnDate == null));
        Assert.Equal(5, GetPatronLoans(data, 3).Count(loan => loan.ReturnDate == null));
        Assert.Equal(4, GetPatronLoans(data, 4).Count(loan => loan.ReturnDate == null));
        Assert.Single(GetPatronLoans(data, 4).Where(loan => loan.ReturnDate != null));

        var availableBookItemCount = data.BookItems.Count(
            bookItem => activeLoans.All(loan => loan.BookItemId != bookItem.Id));
        Assert.Equal(3, availableBookItemCount);

        Assert.True(data.Patrons.Single(patron => patron.Id == 6).MembershipEnd > DateTime.Now);
        Assert.True(data.Patrons.Single(patron => patron.Id == 7).MembershipEnd < DateTime.Now);
        Assert.True(data.Patrons.Single(patron => patron.Id == 10).MembershipEnd < DateTime.Now.AddMonths(1));
    }

    [Fact]
    public async Task InitializeAsync_PreservesExistingRuntimeChanges()
    {
        var firstData = new JsonData(CreateConfiguration());
        await firstData.InitializeAsync();

        firstData.Loans!.First().ReturnDate = DateTime.Now;
        await firstData.SaveLoans(firstData.Loans!);

        var secondData = new JsonData(CreateConfiguration());
        await secondData.InitializeAsync();

        Assert.NotNull(secondData.Loans!.First().ReturnDate);
    }

    [Fact]
    public async Task InitializeAsync_ResetRestoresSeedData()
    {
        var firstData = new JsonData(CreateConfiguration());
        await firstData.InitializeAsync();

        firstData.Loans!.First().ReturnDate = DateTime.Now;
        await firstData.SaveLoans(firstData.Loans!);

        var resetData = new JsonData(CreateConfiguration());
        await resetData.InitializeAsync(resetData: true);

        Assert.Null(resetData.Loans!.First().ReturnDate);
        Assert.True(resetData.Loans!.First().DueDate > DateTime.Now);
    }

    [Fact]
    public async Task InitializeAsync_RejectsPartialRuntimeData()
    {
        var data = new JsonData(CreateConfiguration());
        await data.InitializeAsync();
        File.Delete(Path.Combine(_runtimeRoot, "Loans.json"));

        var partialData = new JsonData(CreateConfiguration());
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => partialData.InitializeAsync());

        Assert.Contains("--reset-data", exception.Message);
    }

    public void Dispose()
    {
        if (Directory.Exists(_runtimeRoot))
        {
            Directory.Delete(_runtimeRoot, recursive: true);
        }
    }

    private IConfiguration CreateConfiguration()
    {
        var repositoryRoot = FindRepositoryRoot();
        var seedRoot = Path.Combine(repositoryRoot, "src", "Library.Console", "Json");

        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JsonData:RuntimeRoot"] = _runtimeRoot,
                ["JsonData:ReferenceDate"] = "2023-12-20",
                ["JsonPaths:Authors"] = Path.Combine(seedRoot, "Authors.json"),
                ["JsonPaths:Books"] = Path.Combine(seedRoot, "Books.json"),
                ["JsonPaths:BookItems"] = Path.Combine(seedRoot, "BookItems.json"),
                ["JsonPaths:Patrons"] = Path.Combine(seedRoot, "Patrons.json"),
                ["JsonPaths:Loans"] = Path.Combine(seedRoot, "Loans.json")
            })
            .Build();
    }

    private static List<Library.ApplicationCore.Entities.Loan> GetPatronLoans(JsonData data, int patronId)
    {
        var patron = data.Patrons!.Single(item => item.Id == patronId);
        return data.GetPopulatedPatron(patron).Loans.ToList();
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AccelerateDevGitHubCopilot.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the repository root.");
    }
}
