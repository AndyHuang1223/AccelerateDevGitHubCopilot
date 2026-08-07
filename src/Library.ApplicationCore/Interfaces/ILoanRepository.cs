using Library.ApplicationCore.Entities;

namespace Library.ApplicationCore;

public interface ILoanRepository
{
    Task AddLoan(Loan loan);
    Task<BookItem?> GetBookItem(int bookItemId);
    Task<List<BookItem>> GetAvailableBookItems();
    Task<Loan?> GetLoan(int loanId);
    Task UpdateLoan(Loan loan);
}