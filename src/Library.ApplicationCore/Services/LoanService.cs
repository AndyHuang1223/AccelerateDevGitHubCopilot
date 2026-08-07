using Library.ApplicationCore;
using Library.ApplicationCore.Entities;
using Library.ApplicationCore.Enums;

public class LoanService : ILoanService
{
    private readonly ILoanRepository _loanRepository;
    private readonly IPatronRepository _patronRepository;
    public const int LoanDurationDays = 14;
    public const int MaxActiveLoans = 5;

    public LoanService(ILoanRepository loanRepository, IPatronRepository patronRepository)
    {
        _loanRepository = loanRepository;
        _patronRepository = patronRepository;
    }

    public async Task<LoanCreationStatus> CreateLoan(int patronId, int bookItemId)
    {
        var patron = await _patronRepository.GetPatron(patronId);
        if (patron == null)
        {
            return LoanCreationStatus.PatronNotFound;
        }

        if (patron.MembershipEnd < DateTime.Now)
        {
            return LoanCreationStatus.MembershipExpired;
        }

        var activeLoanCount = patron.Loans.Count(loan => loan.ReturnDate == null);
        if (activeLoanCount >= MaxActiveLoans)
        {
            return LoanCreationStatus.LoanLimitReached;
        }

        if (patron.Loans.Any(loan => loan.ReturnDate == null && loan.DueDate < DateTime.Now))
        {
            return LoanCreationStatus.PatronHasOverdueLoan;
        }

        var bookItem = await _loanRepository.GetBookItem(bookItemId);
        if (bookItem == null)
        {
            return LoanCreationStatus.BookItemNotFound;
        }

        var availableBookItems = await _loanRepository.GetAvailableBookItems();
        if (!availableBookItems.Any(item => item.Id == bookItemId))
        {
            return LoanCreationStatus.BookItemUnavailable;
        }

        var loanDate = DateTime.Now;
        var loan = new Loan
        {
            PatronId = patron.Id,
            Patron = patron,
            BookItemId = bookItem.Id,
            BookItem = bookItem,
            LoanDate = loanDate,
            DueDate = loanDate.AddDays(LoanDurationDays),
            ReturnDate = null
        };

        try
        {
            await _loanRepository.AddLoan(loan);
            return LoanCreationStatus.Success;
        }
        catch (Exception)
        {
            return LoanCreationStatus.Error;
        }
    }

    public async Task<LoanReturnStatus> ReturnLoan(int loanId)
    {
        Loan? loan = await _loanRepository.GetLoan(loanId);
        if (loan == null)
        {
            return LoanReturnStatus.LoanNotFound;
        }

        // check if already returned
        if (loan.ReturnDate != null)
        {
            return LoanReturnStatus.AlreadyReturned;
        }

        loan.ReturnDate = DateTime.Now;
        try
        {
            await _loanRepository.UpdateLoan(loan);
            return LoanReturnStatus.Success;
        }
        catch (Exception e)
        {
            return LoanReturnStatus.Error;
        }
    }

    public const int ExtendByDays = 14;

    public async Task<LoanExtensionStatus> ExtendLoan(int loanId)
    {
        var loan = await _loanRepository.GetLoan(loanId);

        if (loan == null)
            return LoanExtensionStatus.LoanNotFound;

        // Check if patron's membership is expired
        if (loan.Patron!.MembershipEnd < DateTime.Now)
            return LoanExtensionStatus.MembershipExpired;

        if (loan.ReturnDate != null)
            return LoanExtensionStatus.LoanReturned;

        if (loan.DueDate < DateTime.Now)
            return LoanExtensionStatus.LoanExpired;

        loan.DueDate = loan.DueDate.AddDays(ExtendByDays);
        try
        {
            await _loanRepository.UpdateLoan(loan);
            return LoanExtensionStatus.Success;
        }
        catch (Exception e)
        {
            return LoanExtensionStatus.Error;
        }
    }
}
