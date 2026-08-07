using Library.ApplicationCore;
using Library.ApplicationCore.Entities;

namespace Library.Infrastructure.Data;

public class JsonLoanRepository : ILoanRepository
{
    private readonly JsonData _jsonData;

    public JsonLoanRepository(JsonData jsonData)
    {
        _jsonData = jsonData;
    }

    public async Task AddLoan(Loan loan)
    {
        await _jsonData.EnsureDataLoaded();

        var nextLoanId = _jsonData.Loans!.Count == 0
            ? 1
            : _jsonData.Loans.Max(existingLoan => existingLoan.Id) + 1;

        loan.Id = nextLoanId;
        _jsonData.Loans.Add(new Loan
        {
            Id = loan.Id,
            BookItemId = loan.BookItemId,
            PatronId = loan.PatronId,
            LoanDate = loan.LoanDate,
            DueDate = loan.DueDate,
            ReturnDate = loan.ReturnDate
        });

        await _jsonData.SaveLoans(_jsonData.Loans);
        await _jsonData.LoadData();
    }

    public async Task<BookItem?> GetBookItem(int id)
    {
        await _jsonData.EnsureDataLoaded();

        foreach (BookItem bookItem in _jsonData.BookItems!)
        {
            if (bookItem.Id == id)
            {
                return _jsonData.GetPopulatedBookItem(bookItem);
            }
        }

        return null;
    }

    public async Task<List<BookItem>> GetAvailableBookItems()
    {
        await _jsonData.EnsureDataLoaded();

        var unavailableBookItemIds = _jsonData.Loans!
            .Where(loan => loan.ReturnDate == null)
            .Select(loan => loan.BookItemId)
            .ToHashSet();

        return _jsonData.BookItems!
            .Where(bookItem => !unavailableBookItemIds.Contains(bookItem.Id))
            .Select(_jsonData.GetPopulatedBookItem)
            .ToList();
    }

    public async Task<Loan?> GetLoan(int id)
    {
        await _jsonData.EnsureDataLoaded();

        foreach (Loan loan in _jsonData.Loans!)
        {
            if (loan.Id == id)
            {
                Loan populated = _jsonData.GetPopulatedLoan(loan);
                return populated;
            }
        }
        return null;
    }

    public async Task UpdateLoan(Loan loan)
    {
        Loan? existingLoan = null;
        foreach (Loan l in _jsonData.Loans!)
        {
            if (l.Id == loan.Id)
            {
                existingLoan = l;
                break;
            }
        }

        if (existingLoan != null)
        {
            existingLoan.BookItemId = loan.BookItemId;
            existingLoan.PatronId = loan.PatronId;
            existingLoan.LoanDate = loan.LoanDate;
            existingLoan.DueDate = loan.DueDate;
            existingLoan.ReturnDate = loan.ReturnDate;

            await _jsonData.SaveLoans(_jsonData.Loans!);

            await _jsonData.LoadData();
        }
    }
}