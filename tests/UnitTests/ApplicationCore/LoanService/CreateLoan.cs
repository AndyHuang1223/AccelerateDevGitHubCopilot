using NSubstitute;
using Library.ApplicationCore;
using Library.ApplicationCore.Entities;
using Library.ApplicationCore.Enums;

namespace Library.UnitTests.ApplicationCore.LoanServiceTests;

public class CreateLoanTest
{
  private readonly ILoanRepository _mockLoanRepository;
  private readonly IPatronRepository _mockPatronRepository;
  private readonly LoanService _loanService;

  public CreateLoanTest()
  {
    _mockLoanRepository = Substitute.For<ILoanRepository>();
    _mockPatronRepository = Substitute.For<IPatronRepository>();
    _loanService = new LoanService(_mockLoanRepository, _mockPatronRepository);
  }

  [Fact(DisplayName = "LoanService.CreateLoan: Returns PatronNotFound when patron does not exist")]
  public async Task CreateLoan_ReturnsPatronNotFoundWhenPatronDoesNotExist()
  {
    _mockPatronRepository.GetPatron(42).Returns((Patron?)null);

    var status = await _loanService.CreateLoan(42, 100);

    Assert.Equal(LoanCreationStatus.PatronNotFound, status);
    await _mockLoanRepository.DidNotReceive().AddLoan(Arg.Any<Loan>());
  }

  [Fact(DisplayName = "LoanService.CreateLoan: Returns MembershipExpired when patron membership is expired")]
  public async Task CreateLoan_ReturnsMembershipExpiredWhenPatronMembershipIsExpired()
  {
    var patron = PatronFactory.CreateExpiredPatron();
    var originalLoans = patron.Loans.ToList();
    _mockPatronRepository.GetPatron(patron.Id).Returns(patron);

    var status = await _loanService.CreateLoan(patron.Id, 100);

    Assert.Equal(LoanCreationStatus.MembershipExpired, status);
    Assert.Equal(originalLoans.Count, patron.Loans.Count);
    await _mockLoanRepository.DidNotReceive().AddLoan(Arg.Any<Loan>());
  }

  [Fact(DisplayName = "LoanService.CreateLoan: Creates a first loan when patron has no active loans")]
  public async Task CreateLoan_CreatesFirstLoanWhenPatronHasNoActiveLoans()
  {
    var patron = PatronFactory.CreateCurrentPatronWithLoans(0);
    var bookItem = new BookItem { Id = 100 };
    Loan? createdLoan = null;

    _mockPatronRepository.GetPatron(patron.Id).Returns(patron);
    _mockLoanRepository.GetBookItem(bookItem.Id).Returns(bookItem);
    _mockLoanRepository.GetAvailableBookItems().Returns(new List<BookItem> { bookItem });
    _mockLoanRepository
        .When(repository => repository.AddLoan(Arg.Any<Loan>()))
        .Do(callInfo => createdLoan = callInfo.Arg<Loan>());

    var status = await _loanService.CreateLoan(patron.Id, bookItem.Id);

    Assert.Equal(LoanCreationStatus.Success, status);
    Assert.NotNull(createdLoan);
    await _mockLoanRepository.Received(1).AddLoan(Arg.Any<Loan>());
  }

  [Fact(DisplayName = "LoanService.CreateLoan: Creates a fifth loan when patron has four active loans")]
  public async Task CreateLoan_CreatesFifthLoanWhenPatronHasFourActiveLoans()
  {
    var patron = PatronFactory.CreateCurrentPatronWithLoans(4);
    var bookItem = new BookItem { Id = 100 };
    Loan? createdLoan = null;

    _mockPatronRepository.GetPatron(patron.Id).Returns(patron);
    _mockLoanRepository.GetBookItem(bookItem.Id).Returns(bookItem);
    _mockLoanRepository.GetAvailableBookItems().Returns(new List<BookItem> { bookItem });
    _mockLoanRepository
        .When(repository => repository.AddLoan(Arg.Any<Loan>()))
        .Do(callInfo => createdLoan = callInfo.Arg<Loan>());

    var status = await _loanService.CreateLoan(patron.Id, bookItem.Id);

    Assert.Equal(LoanCreationStatus.Success, status);
    Assert.NotNull(createdLoan);
    await _mockLoanRepository.Received(1).AddLoan(Arg.Any<Loan>());
  }

  [Fact(DisplayName = "LoanService.CreateLoan: Returns LoanLimitReached when patron already has five active loans")]
  public async Task CreateLoan_ReturnsLoanLimitReachedWhenPatronHasFiveActiveLoans()
  {
    var patron = PatronFactory.CreateCurrentPatronWithLoans(5);
    var originalLoans = patron.Loans.ToList();
    _mockPatronRepository.GetPatron(patron.Id).Returns(patron);

    var status = await _loanService.CreateLoan(patron.Id, 100);

    Assert.Equal(LoanCreationStatus.LoanLimitReached, status);
    Assert.Equal(originalLoans.Count, patron.Loans.Count);
    await _mockLoanRepository.DidNotReceive().GetBookItem(Arg.Any<int>());
    await _mockLoanRepository.DidNotReceive().AddLoan(Arg.Any<Loan>());
  }

  [Fact(DisplayName = "LoanService.CreateLoan: Ignores returned loans when checking the active-loan limit")]
  public async Task CreateLoan_IgnoresReturnedLoansWhenCheckingActiveLoanLimit()
  {
    var patron = PatronFactory.CreateCurrentPatronWithLoans(4, returnedLoanCount: 2);
    var bookItem = new BookItem { Id = 100 };
    Loan? createdLoan = null;

    _mockPatronRepository.GetPatron(patron.Id).Returns(patron);
    _mockLoanRepository.GetBookItem(bookItem.Id).Returns(bookItem);
    _mockLoanRepository.GetAvailableBookItems().Returns(new List<BookItem> { bookItem });
    _mockLoanRepository
        .When(repository => repository.AddLoan(Arg.Any<Loan>()))
        .Do(callInfo => createdLoan = callInfo.Arg<Loan>());

    var status = await _loanService.CreateLoan(patron.Id, bookItem.Id);

    Assert.Equal(LoanCreationStatus.Success, status);
    Assert.NotNull(createdLoan);
    await _mockLoanRepository.Received(1).AddLoan(Arg.Any<Loan>());
  }

  [Fact(DisplayName = "LoanService.CreateLoan: Returns BookItemNotFound when the selected book item does not exist")]
  public async Task CreateLoan_ReturnsBookItemNotFoundWhenBookItemDoesNotExist()
  {
    var patron = PatronFactory.CreateCurrentPatronWithLoans(0);
    var originalLoans = patron.Loans.ToList();
    _mockPatronRepository.GetPatron(patron.Id).Returns(patron);
    _mockLoanRepository.GetBookItem(100).Returns((BookItem?)null);

    var status = await _loanService.CreateLoan(patron.Id, 100);

    Assert.Equal(LoanCreationStatus.BookItemNotFound, status);
    Assert.Equal(originalLoans.Count, patron.Loans.Count);
    await _mockLoanRepository.DidNotReceive().GetAvailableBookItems();
    await _mockLoanRepository.DidNotReceive().AddLoan(Arg.Any<Loan>());
  }

  [Fact(DisplayName = "LoanService.CreateLoan: Returns BookItemUnavailable when the book item is already loaned out")]
  public async Task CreateLoan_ReturnsBookItemUnavailableWhenBookItemIsAlreadyLoanedOut()
  {
    var patron = PatronFactory.CreateCurrentPatronWithLoans(1);
    var originalLoans = patron.Loans.ToList();
    var bookItem = new BookItem { Id = 100 };
    _mockPatronRepository.GetPatron(patron.Id).Returns(patron);
    _mockLoanRepository.GetBookItem(bookItem.Id).Returns(bookItem);
    _mockLoanRepository.GetAvailableBookItems().Returns(new List<BookItem>());

    var status = await _loanService.CreateLoan(patron.Id, bookItem.Id);

    Assert.Equal(LoanCreationStatus.BookItemUnavailable, status);
    Assert.Equal(originalLoans.Count, patron.Loans.Count);
    await _mockLoanRepository.DidNotReceive().AddLoan(Arg.Any<Loan>());
  }

  [Fact(DisplayName = "LoanService.CreateLoan: Does not create a loan record when the request is rejected")]
  public async Task CreateLoan_DoesNotCreateLoanRecordWhenRequestIsRejected()
  {
    var patron = PatronFactory.CreateCurrentPatronWithLoans(5);
    _mockPatronRepository.GetPatron(patron.Id).Returns(patron);

    var status = await _loanService.CreateLoan(patron.Id, 100);

    Assert.Equal(LoanCreationStatus.LoanLimitReached, status);
    await _mockLoanRepository.DidNotReceive().AddLoan(Arg.Any<Loan>());
  }

  [Fact(DisplayName = "LoanService.CreateLoan: Leaves patron loan state unchanged when the request is rejected")]
  public async Task CreateLoan_LeavesPatronLoanStateUnchangedWhenRequestIsRejected()
  {
    var patron = PatronFactory.CreateCurrentPatronWithLoans(4, returnedLoanCount: 1);
    var originalLoans = patron.Loans.ToList();
    var bookItem = new BookItem { Id = 100 };

    _mockPatronRepository.GetPatron(patron.Id).Returns(patron);
    _mockLoanRepository.GetBookItem(bookItem.Id).Returns(bookItem);
    _mockLoanRepository.GetAvailableBookItems().Returns(new List<BookItem>());

    var status = await _loanService.CreateLoan(patron.Id, bookItem.Id);

    Assert.Equal(LoanCreationStatus.BookItemUnavailable, status);
    Assert.Equal(originalLoans.Count, patron.Loans.Count);
    Assert.All(originalLoans, loan => Assert.Contains(loan, patron.Loans));
  }

  [Fact(DisplayName = "LoanService.CreateLoan: Populates the new loan fields when a loan is created successfully")]
  public async Task CreateLoan_PopulatesNewLoanFieldsWhenLoanIsCreatedSuccessfully()
  {
    var patron = PatronFactory.CreateCurrentPatronWithLoans(0);
    var bookItem = new BookItem { Id = 100 };
    Loan? createdLoan = null;

    _mockPatronRepository.GetPatron(patron.Id).Returns(patron);
    _mockLoanRepository.GetBookItem(bookItem.Id).Returns(bookItem);
    _mockLoanRepository.GetAvailableBookItems().Returns(new List<BookItem> { bookItem });
    _mockLoanRepository
        .When(repository => repository.AddLoan(Arg.Any<Loan>()))
        .Do(callInfo => createdLoan = callInfo.Arg<Loan>());

    var beforeCreate = DateTime.Now;
    var status = await _loanService.CreateLoan(patron.Id, bookItem.Id);
    var afterCreate = DateTime.Now;

    Assert.Equal(LoanCreationStatus.Success, status);
    Assert.NotNull(createdLoan);
    Assert.Equal(patron.Id, createdLoan!.PatronId);
    Assert.Same(patron, createdLoan.Patron);
    Assert.Equal(bookItem.Id, createdLoan.BookItemId);
    Assert.Same(bookItem, createdLoan.BookItem);
    Assert.InRange(createdLoan.LoanDate, beforeCreate, afterCreate);
    Assert.Equal(createdLoan.LoanDate.AddDays(LoanService.LoanDurationDays), createdLoan.DueDate);
    Assert.Null(createdLoan.ReturnDate);
  }

  [Fact(DisplayName = "LoanService.CreateLoan: Returns Error if saving the new loan fails")]
  public async Task CreateLoan_ReturnsErrorWhenSaveFails()
  {
    var patron = PatronFactory.CreateCurrentPatronWithLoans(0);
    var bookItem = new BookItem { Id = 100 };

    _mockPatronRepository.GetPatron(patron.Id).Returns(patron);
    _mockLoanRepository.GetBookItem(bookItem.Id).Returns(bookItem);
    _mockLoanRepository.GetAvailableBookItems().Returns(new List<BookItem> { bookItem });
    _mockLoanRepository.AddLoan(Arg.Any<Loan>())
        .Returns(Task.FromException(new InvalidOperationException("save failed")));

    var status = await _loanService.CreateLoan(patron.Id, bookItem.Id);

    Assert.Equal(LoanCreationStatus.Error, status);
  }
}