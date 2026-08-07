using System.ComponentModel;

namespace Library.ApplicationCore.Enums;

public enum LoanCreationStatus
{
  [Description("Book loan was created successfully.")]
  Success,

  [Description("Patron not found.")]
  PatronNotFound,

  [Description("Cannot create book loan due to expired patron's membership.")]
  MembershipExpired,

  [Description("Book item not found.")]
  BookItemNotFound,

  [Description("Cannot create book loan because the book item is already loaned out.")]
  BookItemUnavailable,

  [Description("Cannot create book loan because the patron already has 5 active loans.")]
  LoanLimitReached,

  [Description("Cannot create book loan because the patron has an overdue loan.")]
  PatronHasOverdueLoan,

  [Description("Cannot create book loan due to an error.")]
  Error
}
