using Library.ApplicationCore.Entities;
using Library.Infrastructure.Data;
using Microsoft.Extensions.Configuration;

namespace Library.UnitTests.Infrastructure;

public sealed class JsonLoanRepositoryTests : IDisposable
{
  private readonly string _runtimeRoot;

  public JsonLoanRepositoryTests()
  {
    _runtimeRoot = Path.Combine(
        Path.GetTempPath(),
        $"library-json-loan-repository-tests-{Guid.NewGuid():N}");
  }

  [Fact]
  public async Task GetAvailableBookItems_ReturnsOnlyBookItemsWithoutActiveLoans()
  {
    var data = new JsonData(CreateConfiguration());
    await data.InitializeAsync();
    var repository = new JsonLoanRepository(data);

    var activeLoanBookItemIds = data.Loans!
        .Where(loan => loan.ReturnDate == null)
        .Select(loan => loan.BookItemId)
        .ToHashSet();

    var availableBookItems = await repository.GetAvailableBookItems();

    Assert.Equal(3, availableBookItems.Count);
    Assert.DoesNotContain(availableBookItems, bookItem => activeLoanBookItemIds.Contains(bookItem.Id));
    Assert.All(availableBookItems, bookItem => Assert.NotNull(bookItem.Book));
  }

  [Fact]
  public async Task AddLoan_AssignsNextIdPersistsLoanAndRemovesBookItemFromAvailability()
  {
    var data = new JsonData(CreateConfiguration());
    await data.InitializeAsync();
    var repository = new JsonLoanRepository(data);

    var availableBookItem = (await repository.GetAvailableBookItems()).First();
    var existingLoanCount = data.Loans!.Count;
    var maxLoanId = data.Loans.Max(loan => loan.Id);
    var newLoan = new Loan
    {
      PatronId = 1,
      BookItemId = availableBookItem.Id,
      LoanDate = DateTime.Now,
      DueDate = DateTime.Now.AddDays(14),
      ReturnDate = null
    };

    await repository.AddLoan(newLoan);

    Assert.Equal(maxLoanId + 1, newLoan.Id);
    Assert.Equal(existingLoanCount + 1, data.Loans!.Count);

    var savedLoan = await repository.GetLoan(newLoan.Id);
    Assert.NotNull(savedLoan);
    Assert.Equal(newLoan.PatronId, savedLoan!.PatronId);
    Assert.Equal(newLoan.BookItemId, savedLoan.BookItemId);
    Assert.Null(savedLoan.ReturnDate);

    var remainingAvailableBookItems = await repository.GetAvailableBookItems();
    Assert.DoesNotContain(remainingAvailableBookItems, bookItem => bookItem.Id == availableBookItem.Id);
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