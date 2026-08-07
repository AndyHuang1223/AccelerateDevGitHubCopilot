using Library.ApplicationCore.Entities;

public static class LoanFactory
{
    public static int loanId = 777;
    public static int bookItemId = 1000;

    public static Loan CreateReturnedLoanForPatron(Patron patron, int? assignedBookItemId = null)
    {
        var bookItem = assignedBookItemId ?? bookItemId++;
        return new Loan
        {
            Id = loanId++,
            BookItemId = bookItem,
            BookItem = new BookItem { Id = bookItem },
            LoanDate = DateTime.Now.AddDays(-7),
            DueDate = DateTime.Now.AddDays(1),
            ReturnDate = DateTime.Now.AddDays(-1),
            PatronId = patron.Id,
            Patron = patron
        };
    }

    public static Loan CreateCurrentLoanForPatron(Patron patron, int? assignedBookItemId = null)
    {
        var bookItem = assignedBookItemId ?? bookItemId++;
        return new Loan
        {
            Id = loanId++,
            BookItemId = bookItem,
            BookItem = new BookItem { Id = bookItem },
            LoanDate = DateTime.Now.AddDays(-7),
            DueDate = DateTime.Now.AddDays(1),
            ReturnDate = null,
            PatronId = patron.Id,
            Patron = patron
        };
    }

    public static Loan CreateExpiredLoanForPatron(Patron patron, int? assignedBookItemId = null)
    {
        var bookItem = assignedBookItemId ?? bookItemId++;
        return new Loan
        {
            Id = loanId++,
            BookItemId = bookItem,
            BookItem = new BookItem { Id = bookItem },
            LoanDate = DateTime.Now.AddDays(-21),
            DueDate = DateTime.Now.AddDays(-1),
            ReturnDate = null,
            PatronId = patron.Id,
            Patron = patron
        };
    }
}