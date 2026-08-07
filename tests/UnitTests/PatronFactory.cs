using Library.ApplicationCore.Entities;

public static class PatronFactory
{
    public static int patronId = 42;

    public static Patron CreateCurrentPatron()
    {
        return new Patron
        {
            Id = patronId++,
            Name = "John Doe",
            MembershipStart = DateTime.Now.AddYears(-1),
            MembershipEnd = DateTime.Now.AddDays(1),
            Loans = new List<Loan>()
        };
    }

    public static Patron CreateCurrentPatronWithLoans(int activeLoanCount, int returnedLoanCount = 0)
    {
        var patron = CreateCurrentPatron();
        var loans = new List<Loan>();

        for (var i = 0; i < activeLoanCount; i++)
        {
            loans.Add(LoanFactory.CreateCurrentLoanForPatron(patron));
        }

        for (var i = 0; i < returnedLoanCount; i++)
        {
            loans.Add(LoanFactory.CreateReturnedLoanForPatron(patron));
        }

        patron.Loans = loans;
        return patron;
    }

    public static Patron CreateTooEarlyToRenewPatron()
    {
        return new Patron
        {
            Id = patronId++,
            Name = "John Doe",
            MembershipStart = DateTime.Now.AddYears(-1),
            MembershipEnd = DateTime.Now.AddMonths(2),
            Loans = new List<Loan>()
        };
    }

    public static Patron CreateExpiredPatron()
    {
        return new Patron
        {
            Id = patronId++,
            Name = "John Doe",
            MembershipStart = DateTime.Now.AddYears(-2),
            MembershipEnd = DateTime.Now.AddMonths(-2),
            Loans = new List<Loan>()
        };
    }
}