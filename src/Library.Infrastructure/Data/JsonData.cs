using System.Globalization;
using System.Text.Json;
using Library.ApplicationCore.Entities;
using Microsoft.Extensions.Configuration;

namespace Library.Infrastructure.Data;

public class JsonData
{
    public List<Author>? Authors { get; set; }
    public List<Book>? Books { get; set; }
    public List<BookItem>? BookItems { get; set; }
    public List<Patron>? Patrons { get; set; }
    public List<Loan>? Loans { get; set; }

    private readonly string _authorsSeedPath;
    private readonly string _booksSeedPath;
    private readonly string _bookItemsSeedPath;
    private readonly string _patronsSeedPath;
    private readonly string _loansSeedPath;

    private readonly string _authorsPath;
    private readonly string _booksPath;
    private readonly string _bookItemsPath;
    private readonly string _patronsPath;
    private readonly string _loansPath;
    private readonly string _runtimeDataRootPath;
    private readonly TimeSpan _dateOffset;

    public JsonData(IConfiguration configuration)
    {
        var paths = configuration.GetSection("JsonPaths");

        _authorsSeedPath = ResolveSeedPath(paths["Authors"], "Authors.json");
        _booksSeedPath = ResolveSeedPath(paths["Books"], "Books.json");
        _bookItemsSeedPath = ResolveSeedPath(paths["BookItems"], "BookItems.json");
        _patronsSeedPath = ResolveSeedPath(paths["Patrons"], "Patrons.json");
        _loansSeedPath = ResolveSeedPath(paths["Loans"], "Loans.json");

        var configuredRuntimeRoot = configuration["JsonData:RuntimeRoot"] ?? ".library-data";
        _runtimeDataRootPath = Path.GetFullPath(configuredRuntimeRoot, Directory.GetCurrentDirectory());

        var referenceDateText = configuration["JsonData:ReferenceDate"] ?? "2023-12-20";
        var referenceDate = DateTime.ParseExact(
            referenceDateText,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None);
        _dateOffset = DateTime.Today - referenceDate.Date;

        _authorsPath = GetRuntimePath(_authorsSeedPath);
        _booksPath = GetRuntimePath(_booksSeedPath);
        _bookItemsPath = GetRuntimePath(_bookItemsSeedPath);
        _patronsPath = GetRuntimePath(_patronsSeedPath);
        _loansPath = GetRuntimePath(_loansSeedPath);
    }

    public string RuntimeDataRootPath => _runtimeDataRootPath;

    public async Task InitializeAsync(bool resetData = false)
    {
        var runtimePaths = GetRuntimePaths();
        var existingFiles = runtimePaths.Count(File.Exists);

        if (resetData)
        {
            DeleteRuntimeFiles(runtimePaths);
            await CreateRuntimeData();
        }
        else if (existingFiles == 0)
        {
            await CreateRuntimeData();
        }
        else if (existingFiles != runtimePaths.Count)
        {
            throw new InvalidOperationException(
                $"Runtime data directory '{_runtimeDataRootPath}' is incomplete. " +
                "Run with --reset-data to recreate the lab data.");
        }

        await LoadData();
    }

    public async Task EnsureDataLoaded()
    {
        if (Patrons == null)
        {
            await InitializeAsync();
        }
    }

    public async Task LoadData()
    {
        Authors = await LoadJson<List<Author>>(_authorsPath);
        Books = await LoadJson<List<Book>>(_booksPath);
        BookItems = await LoadJson<List<BookItem>>(_bookItemsPath);
        Patrons = await LoadJson<List<Patron>>(_patronsPath);
        Loans = await LoadJson<List<Loan>>(_loansPath);
    }

    public async Task SaveLoans(IEnumerable<Loan> loans)
    {
        List<Loan> loanList = new List<Loan>();
        foreach (var l in loans)
        {
            Loan loan = new Loan
            {
                // making sure only a subset of properties is set and saved
                Id = l.Id,
                BookItemId = l.BookItemId,
                PatronId = l.PatronId,
                LoanDate = l.LoanDate,
                DueDate = l.DueDate,
                ReturnDate = l.ReturnDate
            };
            loanList.Add(loan);
        }
        await SaveJson(_loansPath, loanList);
    }

    public async Task SavePatrons(IEnumerable<Patron> patrons)
    {
        await SaveJson(_patronsPath, patrons.Select(p => new Patron
        {
            Id = p.Id,
            Name = p.Name,
            MembershipStart = p.MembershipStart,
            MembershipEnd = p.MembershipEnd,
            ImageName = p.ImageName,
        }).ToList());
    }

    public List<Patron> GetPopulatedPatrons(IEnumerable<Patron> patrons) =>
        patrons.Select(GetPopulatedPatron).ToList();

    public Patron GetPopulatedPatron(Patron p)
    {
        Patron populated = new Patron
        {
            Id = p.Id,
            Name = p.Name,
            ImageName = p.ImageName,
            MembershipStart = p.MembershipStart,
            MembershipEnd = p.MembershipEnd,
            Loans = new List<Loan>()
        };

        foreach (Loan loan in Loans!)
        {
            if (loan.PatronId == p.Id)
            {
                populated.Loans.Add(GetPopulatedLoan(loan));
            }
        }

        return populated;
    }

    public Loan GetPopulatedLoan(Loan l)
    {
        return new Loan
        {
            Id = l.Id,
            BookItemId = l.BookItemId,
            PatronId = l.PatronId,
            LoanDate = l.LoanDate,
            DueDate = l.DueDate,
            ReturnDate = l.ReturnDate,
            BookItem = GetPopulatedBookItem(BookItems!.Single(bi => bi.Id == l.BookItemId)),
            Patron = Patrons!.Single(p => p.Id == l.PatronId)
        };
    }

    public BookItem GetPopulatedBookItem(BookItem bi)
    {
        return new BookItem
        {
            Id = bi.Id,
            BookId = bi.BookId,
            AcquisitionDate = bi.AcquisitionDate,
            Condition = bi.Condition,
            Book = GetPopulatedBook(Books!.Single(b => b.Id == bi.BookId))
        };
    }

    public Book GetPopulatedBook(Book b)
    {
        return new Book
        {
            Id = b.Id,
            Title = b.Title,
            AuthorId = b.AuthorId,
            Genre = b.Genre,
            ISBN = b.ISBN,
            ImageName = b.ImageName,
            Author = Authors!
                .Where(a => a.Id == b.AuthorId)
                .Select(a => new Author { Id = a.Id, Name = a.Name })
                .First()
        };
    }

    private async Task CreateRuntimeData()
    {
        Directory.CreateDirectory(_runtimeDataRootPath);
        var runtimePaths = GetRuntimePaths();

        try
        {
            var authors = await LoadJson<List<Author>>(_authorsSeedPath);
            var books = await LoadJson<List<Book>>(_booksSeedPath);
            var bookItems = await LoadJson<List<BookItem>>(_bookItemsSeedPath);
            var patrons = await LoadJson<List<Patron>>(_patronsSeedPath);
            var loans = await LoadJson<List<Loan>>(_loansSeedPath);

            foreach (var bookItem in bookItems)
            {
                bookItem.AcquisitionDate = Shift(bookItem.AcquisitionDate);
            }

            foreach (var patron in patrons)
            {
                patron.MembershipStart = Shift(patron.MembershipStart);
                patron.MembershipEnd = Shift(patron.MembershipEnd);
            }

            foreach (var loan in loans)
            {
                loan.LoanDate = Shift(loan.LoanDate);
                loan.DueDate = Shift(loan.DueDate);
                loan.ReturnDate = Shift(loan.ReturnDate);
            }

            await SaveJson(_authorsPath, authors);
            await SaveJson(_booksPath, books);
            await SaveJson(_bookItemsPath, bookItems);
            await SaveJson(_patronsPath, patrons);
            await SaveJson(_loansPath, loans);
        }
        catch
        {
            DeleteRuntimeFiles(runtimePaths);
            throw;
        }
    }

    private DateTime Shift(DateTime value) => value + _dateOffset;

    private DateTime? Shift(DateTime? value) => value.HasValue ? Shift(value.Value) : null;

    private async Task<T> LoadJson<T>(string filePath)
    {
        await using FileStream jsonStream = File.OpenRead(filePath);
        return await JsonSerializer.DeserializeAsync<T>(jsonStream)
            ?? throw new InvalidOperationException($"JSON file '{filePath}' is empty or invalid.");
    }

    private async Task SaveJson<T>(string filePath, T data)
    {
        await using FileStream jsonStream = File.Create(filePath);
        await JsonSerializer.SerializeAsync(jsonStream, data);
    }

    private string ResolveSeedPath(string? configuredPath, string defaultFileName)
    {
        var path = configuredPath ?? Path.Combine("Json", defaultFileName);

        if (Path.IsPathRooted(path))
        {
            return path;
        }

        return Path.GetFullPath(path, AppContext.BaseDirectory);
    }

    private string GetRuntimePath(string seedPath) =>
        Path.Combine(_runtimeDataRootPath, Path.GetFileName(seedPath));

    private IReadOnlyList<string> GetRuntimePaths() =>
        new[] { _authorsPath, _booksPath, _bookItemsPath, _patronsPath, _loansPath };

    private static void DeleteRuntimeFiles(IEnumerable<string> runtimePaths)
    {
        foreach (var path in runtimePaths)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
