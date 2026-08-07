using Library.ApplicationCore.Enums;

public interface ILoanService
{
    Task<LoanCreationStatus> CreateLoan(int patronId, int bookItemId);
    Task<LoanReturnStatus> ReturnLoan(int loanId);
    Task<LoanExtensionStatus> ExtendLoan(int loanId);
}