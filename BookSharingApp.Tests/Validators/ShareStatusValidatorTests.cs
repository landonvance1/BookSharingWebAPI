using BookSharingApp.Common;
using BookSharingApp.Tests.Helpers;
using BookSharingApp.Validators;
using FluentAssertions;

namespace BookSharingApp.Tests.Validators
{
    public class ShareStatusValidatorTests
    {
        private readonly ShareStatusValidator _validator;

        public ShareStatusValidatorTests()
        {
            _validator = new ShareStatusValidator();
        }

        #region Authorization Tests

        [Fact]
        public void ValidateStatusTransition_WhenUserIsNeitherLenderNorBorrower_ReturnsFailure()
        {
            // Arrange
            var lender = TestDataBuilder.CreateUser(id: "lender-1");
            var borrower = TestDataBuilder.CreateUser(id: "borrower-1");
            var unauthorizedUser = "unauthorized-user";
            var book = TestDataBuilder.CreateBook();
            var userBook = TestDataBuilder.CreateUserBook(userId: lender.Id, bookId: book.Id, user: lender, book: book);
            var share = TestDataBuilder.CreateShare(
                userBookId: userBook.Id,
                borrower: borrower.Id,
                status: ShareStatus.Requested,
                userBook: userBook,
                borrowerUser: borrower
            );

            // Act
            var result = _validator.ValidateStatusTransition(share, ShareStatus.Ready, unauthorizedUser);

            // Assert
            result.IsValid.Should().BeFalse();
            result.ErrorMessage.Should().Be("You are not authorized to update this share");
        }

        [Fact]
        public void ValidateStatusTransition_WhenLenderUpdatesTheirShare_ReturnsSuccess()
        {
            // Arrange
            var lender = TestDataBuilder.CreateUser(id: "lender-1");
            var borrower = TestDataBuilder.CreateUser(id: "borrower-1");
            var book = TestDataBuilder.CreateBook();
            var userBook = TestDataBuilder.CreateUserBook(userId: lender.Id, bookId: book.Id, user: lender, book: book);
            var share = TestDataBuilder.CreateShare(
                userBookId: userBook.Id,
                borrower: borrower.Id,
                status: ShareStatus.Requested,
                userBook: userBook,
                borrowerUser: borrower
            );

            // Act
            var result = _validator.ValidateStatusTransition(share, ShareStatus.Ready, lender.Id);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void ValidateStatusTransition_WhenBorrowerUpdatesTheirShare_ReturnsSuccess()
        {
            // Arrange
            var lender = TestDataBuilder.CreateUser(id: "lender-1");
            var borrower = TestDataBuilder.CreateUser(id: "borrower-1");
            var book = TestDataBuilder.CreateBook();
            var userBook = TestDataBuilder.CreateUserBook(userId: lender.Id, bookId: book.Id, user: lender, book: book);
            var share = TestDataBuilder.CreateShare(
                userBookId: userBook.Id,
                borrower: borrower.Id,
                status: ShareStatus.Ready,
                userBook: userBook,
                borrowerUser: borrower
            );

            // Act
            var result = _validator.ValidateStatusTransition(share, ShareStatus.PickedUp, borrower.Id);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        #endregion

        #region Lender-Only Actions

        [Fact]
        public void ValidateStatusTransition_WhenBorrowerTriesToSetReady_ReturnsFailure()
        {
            // Arrange
            var lender = TestDataBuilder.CreateUser(id: "lender-1");
            var borrower = TestDataBuilder.CreateUser(id: "borrower-1");
            var book = TestDataBuilder.CreateBook();
            var userBook = TestDataBuilder.CreateUserBook(userId: lender.Id, bookId: book.Id, user: lender, book: book);
            var share = TestDataBuilder.CreateShare(
                userBookId: userBook.Id,
                borrower: borrower.Id,
                status: ShareStatus.Requested,
                userBook: userBook,
                borrowerUser: borrower
            );

            // Act
            var result = _validator.ValidateStatusTransition(share, ShareStatus.Ready, borrower.Id);

            // Assert
            result.IsValid.Should().BeFalse();
            result.ErrorMessage.Should().Be("Only the lender can update status to Declined, Ready, or Home Safe");
        }

        [Fact]
        public void ValidateStatusTransition_WhenBorrowerTriesToSetHomeSafe_ReturnsFailure()
        {
            // Arrange
            var lender = TestDataBuilder.CreateUser(id: "lender-1");
            var borrower = TestDataBuilder.CreateUser(id: "borrower-1");
            var book = TestDataBuilder.CreateBook();
            var userBook = TestDataBuilder.CreateUserBook(userId: lender.Id, bookId: book.Id, user: lender, book: book);
            var share = TestDataBuilder.CreateShare(
                userBookId: userBook.Id,
                borrower: borrower.Id,
                status: ShareStatus.Returned,
                userBook: userBook,
                borrowerUser: borrower
            );

            // Act
            var result = _validator.ValidateStatusTransition(share, ShareStatus.HomeSafe, borrower.Id);

            // Assert
            result.IsValid.Should().BeFalse();
            result.ErrorMessage.Should().Be("Only the lender can update status to Declined, Ready, or Home Safe");
        }

        [Fact]
        public void ValidateStatusTransition_WhenBorrowerTriesToDecline_ReturnsFailure()
        {
            // Arrange
            var lender = TestDataBuilder.CreateUser(id: "lender-1");
            var borrower = TestDataBuilder.CreateUser(id: "borrower-1");
            var book = TestDataBuilder.CreateBook();
            var userBook = TestDataBuilder.CreateUserBook(userId: lender.Id, bookId: book.Id, user: lender, book: book);
            var share = TestDataBuilder.CreateShare(
                userBookId: userBook.Id,
                borrower: borrower.Id,
                status: ShareStatus.Requested,
                userBook: userBook,
                borrowerUser: borrower
            );

            // Act
            var result = _validator.ValidateStatusTransition(share, ShareStatus.Declined, borrower.Id);

            // Assert
            result.IsValid.Should().BeFalse();
            result.ErrorMessage.Should().Be("Only the lender can update status to Declined, Ready, or Home Safe");
        }

        [Fact]
        public void ValidateStatusTransition_WhenLenderSetsReady_ReturnsSuccess()
        {
            // Arrange
            var lender = TestDataBuilder.CreateUser(id: "lender-1");
            var borrower = TestDataBuilder.CreateUser(id: "borrower-1");
            var book = TestDataBuilder.CreateBook();
            var userBook = TestDataBuilder.CreateUserBook(userId: lender.Id, bookId: book.Id, user: lender, book: book);
            var share = TestDataBuilder.CreateShare(
                userBookId: userBook.Id,
                borrower: borrower.Id,
                status: ShareStatus.Requested,
                userBook: userBook,
                borrowerUser: borrower
            );

            // Act
            var result = _validator.ValidateStatusTransition(share, ShareStatus.Ready, lender.Id);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void ValidateStatusTransition_WhenLenderSetsHomeSafe_ReturnsSuccess()
        {
            // Arrange
            var lender = TestDataBuilder.CreateUser(id: "lender-1");
            var borrower = TestDataBuilder.CreateUser(id: "borrower-1");
            var book = TestDataBuilder.CreateBook();
            var userBook = TestDataBuilder.CreateUserBook(userId: lender.Id, bookId: book.Id, user: lender, book: book);
            var share = TestDataBuilder.CreateShare(
                userBookId: userBook.Id,
                borrower: borrower.Id,
                status: ShareStatus.Returned,
                userBook: userBook,
                borrowerUser: borrower
            );

            // Act
            var result = _validator.ValidateStatusTransition(share, ShareStatus.HomeSafe, lender.Id);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void ValidateStatusTransition_WhenLenderDeclinesRequest_ReturnsSuccess()
        {
            // Arrange
            var lender = TestDataBuilder.CreateUser(id: "lender-1");
            var borrower = TestDataBuilder.CreateUser(id: "borrower-1");
            var book = TestDataBuilder.CreateBook();
            var userBook = TestDataBuilder.CreateUserBook(userId: lender.Id, bookId: book.Id, user: lender, book: book);
            var share = TestDataBuilder.CreateShare(
                userBookId: userBook.Id,
                borrower: borrower.Id,
                status: ShareStatus.Requested,
                userBook: userBook,
                borrowerUser: borrower
            );

            // Act
            var result = _validator.ValidateStatusTransition(share, ShareStatus.Declined, lender.Id);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        #endregion

        #region Borrower-Only Actions

        [Fact]
        public void ValidateStatusTransition_WhenLenderTriesToSetPickedUp_ReturnsFailure()
        {
            // Arrange
            var lender = TestDataBuilder.CreateUser(id: "lender-1");
            var borrower = TestDataBuilder.CreateUser(id: "borrower-1");
            var book = TestDataBuilder.CreateBook();
            var userBook = TestDataBuilder.CreateUserBook(userId: lender.Id, bookId: book.Id, user: lender, book: book);
            var share = TestDataBuilder.CreateShare(
                userBookId: userBook.Id,
                borrower: borrower.Id,
                status: ShareStatus.Ready,
                userBook: userBook,
                borrowerUser: borrower
            );

            // Act
            var result = _validator.ValidateStatusTransition(share, ShareStatus.PickedUp, lender.Id);

            // Assert
            result.IsValid.Should().BeFalse();
            result.ErrorMessage.Should().Be("Only the borrower can update status to Picked Up or Returned");
        }

        [Fact]
        public void ValidateStatusTransition_WhenLenderTriesToSetReturned_ReturnsFailure()
        {
            // Arrange
            var lender = TestDataBuilder.CreateUser(id: "lender-1");
            var borrower = TestDataBuilder.CreateUser(id: "borrower-1");
            var book = TestDataBuilder.CreateBook();
            var userBook = TestDataBuilder.CreateUserBook(userId: lender.Id, bookId: book.Id, user: lender, book: book);
            var share = TestDataBuilder.CreateShare(
                userBookId: userBook.Id,
                borrower: borrower.Id,
                status: ShareStatus.PickedUp,
                userBook: userBook,
                borrowerUser: borrower
            );

            // Act
            var result = _validator.ValidateStatusTransition(share, ShareStatus.Returned, lender.Id);

            // Assert
            result.IsValid.Should().BeFalse();
            result.ErrorMessage.Should().Be("Only the borrower can update status to Picked Up or Returned");
        }

        [Fact]
        public void ValidateStatusTransition_WhenBorrowerSetsPickedUp_ReturnsSuccess()
        {
            // Arrange
            var lender = TestDataBuilder.CreateUser(id: "lender-1");
            var borrower = TestDataBuilder.CreateUser(id: "borrower-1");
            var book = TestDataBuilder.CreateBook();
            var userBook = TestDataBuilder.CreateUserBook(userId: lender.Id, bookId: book.Id, user: lender, book: book);
            var share = TestDataBuilder.CreateShare(
                userBookId: userBook.Id,
                borrower: borrower.Id,
                status: ShareStatus.Ready,
                userBook: userBook,
                borrowerUser: borrower
            );

            // Act
            var result = _validator.ValidateStatusTransition(share, ShareStatus.PickedUp, borrower.Id);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void ValidateStatusTransition_WhenBorrowerSetsReturned_ReturnsSuccess()
        {
            // Arrange
            var lender = TestDataBuilder.CreateUser(id: "lender-1");
            var borrower = TestDataBuilder.CreateUser(id: "borrower-1");
            var book = TestDataBuilder.CreateBook();
            var userBook = TestDataBuilder.CreateUserBook(userId: lender.Id, bookId: book.Id, user: lender, book: book);
            var share = TestDataBuilder.CreateShare(
                userBookId: userBook.Id,
                borrower: borrower.Id,
                status: ShareStatus.PickedUp,
                userBook: userBook,
                borrowerUser: borrower
            );

            // Act
            var result = _validator.ValidateStatusTransition(share, ShareStatus.Returned, borrower.Id);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        #endregion

        #region Valid Status Transitions

        [Fact]
        public void ValidateStatusTransition_FromRequestedToReady_ReturnsSuccess()
        {
            // Arrange
            var lender = TestDataBuilder.CreateUser(id: "lender-1");
            var borrower = TestDataBuilder.CreateUser(id: "borrower-1");
            var book = TestDataBuilder.CreateBook();
            var userBook = TestDataBuilder.CreateUserBook(userId: lender.Id, bookId: book.Id, user: lender, book: book);
            var share = TestDataBuilder.CreateShare(
                userBookId: userBook.Id,
                borrower: borrower.Id,
                status: ShareStatus.Requested,
                userBook: userBook,
                borrowerUser: borrower
            );

            // Act
            var result = _validator.ValidateStatusTransition(share, ShareStatus.Ready, lender.Id);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void ValidateStatusTransition_FromRequestedToDeclined_ReturnsSuccess()
        {
            // Arrange
            var lender = TestDataBuilder.CreateUser(id: "lender-1");
            var borrower = TestDataBuilder.CreateUser(id: "borrower-1");
            var book = TestDataBuilder.CreateBook();
            var userBook = TestDataBuilder.CreateUserBook(userId: lender.Id, bookId: book.Id, user: lender, book: book);
            var share = TestDataBuilder.CreateShare(
                userBookId: userBook.Id,
                borrower: borrower.Id,
                status: ShareStatus.Requested,
                userBook: userBook,
                borrowerUser: borrower
            );

            // Act
            var result = _validator.ValidateStatusTransition(share, ShareStatus.Declined, lender.Id);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void ValidateStatusTransition_FromReadyToPickedUp_ReturnsSuccess()
        {
            // Arrange
            var lender = TestDataBuilder.CreateUser(id: "lender-1");
            var borrower = TestDataBuilder.CreateUser(id: "borrower-1");
            var book = TestDataBuilder.CreateBook();
            var userBook = TestDataBuilder.CreateUserBook(userId: lender.Id, bookId: book.Id, user: lender, book: book);
            var share = TestDataBuilder.CreateShare(
                userBookId: userBook.Id,
                borrower: borrower.Id,
                status: ShareStatus.Ready,
                userBook: userBook,
                borrowerUser: borrower
            );

            // Act
            var result = _validator.ValidateStatusTransition(share, ShareStatus.PickedUp, borrower.Id);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void ValidateStatusTransition_FromPickedUpToReturned_ReturnsSuccess()
        {
            // Arrange
            var lender = TestDataBuilder.CreateUser(id: "lender-1");
            var borrower = TestDataBuilder.CreateUser(id: "borrower-1");
            var book = TestDataBuilder.CreateBook();
            var userBook = TestDataBuilder.CreateUserBook(userId: lender.Id, bookId: book.Id, user: lender, book: book);
            var share = TestDataBuilder.CreateShare(
                userBookId: userBook.Id,
                borrower: borrower.Id,
                status: ShareStatus.PickedUp,
                userBook: userBook,
                borrowerUser: borrower
            );

            // Act
            var result = _validator.ValidateStatusTransition(share, ShareStatus.Returned, borrower.Id);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void ValidateStatusTransition_FromReturnedToHomeSafe_ReturnsSuccess()
        {
            // Arrange
            var lender = TestDataBuilder.CreateUser(id: "lender-1");
            var borrower = TestDataBuilder.CreateUser(id: "borrower-1");
            var book = TestDataBuilder.CreateBook();
            var userBook = TestDataBuilder.CreateUserBook(userId: lender.Id, bookId: book.Id, user: lender, book: book);
            var share = TestDataBuilder.CreateShare(
                userBookId: userBook.Id,
                borrower: borrower.Id,
                status: ShareStatus.Returned,
                userBook: userBook,
                borrowerUser: borrower
            );

            // Act
            var result = _validator.ValidateStatusTransition(share, ShareStatus.HomeSafe, lender.Id);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        #endregion

        #region Invalid Status Transitions - Backwards

        [Fact]
        public void ValidateStatusTransition_WhenMovingBackwards_ReturnsFailure()
        {
            // Arrange
            var lender = TestDataBuilder.CreateUser(id: "lender-1");
            var borrower = TestDataBuilder.CreateUser(id: "borrower-1");
            var book = TestDataBuilder.CreateBook();
            var userBook = TestDataBuilder.CreateUserBook(userId: lender.Id, bookId: book.Id, user: lender, book: book);
            var share = TestDataBuilder.CreateShare(
                userBookId: userBook.Id,
                borrower: borrower.Id,
                status: ShareStatus.PickedUp,
                userBook: userBook,
                borrowerUser: borrower
            );

            // Act
            var result = _validator.ValidateStatusTransition(share, ShareStatus.Ready, lender.Id);

            // Assert
            result.IsValid.Should().BeFalse();
            result.ErrorMessage.Should().Be("Invalid status transition from PickedUp to Ready");
        }

        [Fact]
        public void ValidateStatusTransition_WhenStayingSameStatus_ReturnsFailure()
        {
            // Arrange
            var lender = TestDataBuilder.CreateUser(id: "lender-1");
            var borrower = TestDataBuilder.CreateUser(id: "borrower-1");
            var book = TestDataBuilder.CreateBook();
            var userBook = TestDataBuilder.CreateUserBook(userId: lender.Id, bookId: book.Id, user: lender, book: book);
            var share = TestDataBuilder.CreateShare(
                userBookId: userBook.Id,
                borrower: borrower.Id,
                status: ShareStatus.Ready,
                userBook: userBook,
                borrowerUser: borrower
            );

            // Act
            var result = _validator.ValidateStatusTransition(share, ShareStatus.Ready, lender.Id);

            // Assert
            result.IsValid.Should().BeFalse();
            result.ErrorMessage.Should().Be("Invalid status transition from Ready to Ready");
        }

        #endregion

        #region Invalid Status Transitions - Skipping Stages

        [Fact]
        public void ValidateStatusTransition_FromRequestedToPickedUp_ReturnsFailure()
        {
            // Arrange
            var lender = TestDataBuilder.CreateUser(id: "lender-1");
            var borrower = TestDataBuilder.CreateUser(id: "borrower-1");
            var book = TestDataBuilder.CreateBook();
            var userBook = TestDataBuilder.CreateUserBook(userId: lender.Id, bookId: book.Id, user: lender, book: book);
            var share = TestDataBuilder.CreateShare(
                userBookId: userBook.Id,
                borrower: borrower.Id,
                status: ShareStatus.Requested,
                userBook: userBook,
                borrowerUser: borrower
            );

            // Act
            var result = _validator.ValidateStatusTransition(share, ShareStatus.PickedUp, borrower.Id);

            // Assert
            result.IsValid.Should().BeFalse();
            result.ErrorMessage.Should().Be("Invalid status transition from Requested to PickedUp");
        }

        [Fact]
        public void ValidateStatusTransition_FromReadyToReturned_ReturnsFailure()
        {
            // Arrange
            var lender = TestDataBuilder.CreateUser(id: "lender-1");
            var borrower = TestDataBuilder.CreateUser(id: "borrower-1");
            var book = TestDataBuilder.CreateBook();
            var userBook = TestDataBuilder.CreateUserBook(userId: lender.Id, bookId: book.Id, user: lender, book: book);
            var share = TestDataBuilder.CreateShare(
                userBookId: userBook.Id,
                borrower: borrower.Id,
                status: ShareStatus.Ready,
                userBook: userBook,
                borrowerUser: borrower
            );

            // Act
            var result = _validator.ValidateStatusTransition(share, ShareStatus.Returned, borrower.Id);

            // Assert
            result.IsValid.Should().BeFalse();
            result.ErrorMessage.Should().Be("Invalid status transition from Ready to Returned");
        }

        [Fact]
        public void ValidateStatusTransition_FromRequestedToHomeSafe_ReturnsFailure()
        {
            // Arrange
            var lender = TestDataBuilder.CreateUser(id: "lender-1");
            var borrower = TestDataBuilder.CreateUser(id: "borrower-1");
            var book = TestDataBuilder.CreateBook();
            var userBook = TestDataBuilder.CreateUserBook(userId: lender.Id, bookId: book.Id, user: lender, book: book);
            var share = TestDataBuilder.CreateShare(
                userBookId: userBook.Id,
                borrower: borrower.Id,
                status: ShareStatus.Requested,
                userBook: userBook,
                borrowerUser: borrower
            );

            // Act
            var result = _validator.ValidateStatusTransition(share, ShareStatus.HomeSafe, lender.Id);

            // Assert
            result.IsValid.Should().BeFalse();
            result.ErrorMessage.Should().Be("Invalid status transition from Requested to HomeSafe");
        }

        #endregion

        #region Terminal States Cannot Transition

        [Fact]
        public void ValidateStatusTransition_FromHomeSafeToAnyStatus_ReturnsFailure()
        {
            // Arrange
            var lender = TestDataBuilder.CreateUser(id: "lender-1");
            var borrower = TestDataBuilder.CreateUser(id: "borrower-1");
            var book = TestDataBuilder.CreateBook();
            var userBook = TestDataBuilder.CreateUserBook(userId: lender.Id, bookId: book.Id, user: lender, book: book);
            var share = TestDataBuilder.CreateShare(
                userBookId: userBook.Id,
                borrower: borrower.Id,
                status: ShareStatus.HomeSafe,
                userBook: userBook,
                borrowerUser: borrower
            );

            // Act
            var result = _validator.ValidateStatusTransition(share, ShareStatus.Requested, lender.Id);

            // Assert
            result.IsValid.Should().BeFalse();
            result.ErrorMessage.Should().Be("Invalid status transition from HomeSafe to Requested");
        }

        [Fact]
        public void ValidateStatusTransition_FromDeclinedToAnyStatus_ReturnsFailure()
        {
            // Arrange
            var lender = TestDataBuilder.CreateUser(id: "lender-1");
            var borrower = TestDataBuilder.CreateUser(id: "borrower-1");
            var book = TestDataBuilder.CreateBook();
            var userBook = TestDataBuilder.CreateUserBook(userId: lender.Id, bookId: book.Id, user: lender, book: book);
            var share = TestDataBuilder.CreateShare(
                userBookId: userBook.Id,
                borrower: borrower.Id,
                status: ShareStatus.Declined,
                userBook: userBook,
                borrowerUser: borrower
            );

            // Act
            var result = _validator.ValidateStatusTransition(share, ShareStatus.Ready, lender.Id);

            // Assert
            result.IsValid.Should().BeFalse();
            result.ErrorMessage.Should().Be("Invalid status transition from Declined to Ready");
        }

        [Fact]
        public void ValidateStatusTransition_FromDisputedToAnyStatus_ReturnsFailure()
        {
            // Arrange
            var lender = TestDataBuilder.CreateUser(id: "lender-1");
            var borrower = TestDataBuilder.CreateUser(id: "borrower-1");
            var book = TestDataBuilder.CreateBook();
            var userBook = TestDataBuilder.CreateUserBook(userId: lender.Id, bookId: book.Id, user: lender, book: book);
            var share = TestDataBuilder.CreateShare(
                userBookId: userBook.Id,
                borrower: borrower.Id,
                status: ShareStatus.Disputed,
                userBook: userBook,
                borrowerUser: borrower
            );

            // Act
            var result = _validator.ValidateStatusTransition(share, ShareStatus.HomeSafe, lender.Id);

            // Assert
            result.IsValid.Should().BeFalse();
            result.ErrorMessage.Should().Be("Invalid status transition from Disputed to HomeSafe");
        }

        #endregion

        #region Disputed Status Special Cases

        [Fact]
        public void ValidateStatusTransition_ToDisputedFromRequested_ReturnsSuccess()
        {
            // Arrange
            var lender = TestDataBuilder.CreateUser(id: "lender-1");
            var borrower = TestDataBuilder.CreateUser(id: "borrower-1");
            var book = TestDataBuilder.CreateBook();
            var userBook = TestDataBuilder.CreateUserBook(userId: lender.Id, bookId: book.Id, user: lender, book: book);
            var share = TestDataBuilder.CreateShare(
                userBookId: userBook.Id,
                borrower: borrower.Id,
                status: ShareStatus.Requested,
                userBook: userBook,
                borrowerUser: borrower
            );

            // Act - Either party can dispute
            var resultLender = _validator.ValidateStatusTransition(share, ShareStatus.Disputed, lender.Id);
            var resultBorrower = _validator.ValidateStatusTransition(share, ShareStatus.Disputed, borrower.Id);

            // Assert
            resultLender.IsValid.Should().BeTrue();
            resultBorrower.IsValid.Should().BeTrue();
        }

        [Fact]
        public void ValidateStatusTransition_ToDisputedFromReady_ReturnsSuccess()
        {
            // Arrange
            var lender = TestDataBuilder.CreateUser(id: "lender-1");
            var borrower = TestDataBuilder.CreateUser(id: "borrower-1");
            var book = TestDataBuilder.CreateBook();
            var userBook = TestDataBuilder.CreateUserBook(userId: lender.Id, bookId: book.Id, user: lender, book: book);
            var share = TestDataBuilder.CreateShare(
                userBookId: userBook.Id,
                borrower: borrower.Id,
                status: ShareStatus.Ready,
                userBook: userBook,
                borrowerUser: borrower
            );

            // Act
            var result = _validator.ValidateStatusTransition(share, ShareStatus.Disputed, borrower.Id);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void ValidateStatusTransition_ToDisputedFromPickedUp_ReturnsSuccess()
        {
            // Arrange
            var lender = TestDataBuilder.CreateUser(id: "lender-1");
            var borrower = TestDataBuilder.CreateUser(id: "borrower-1");
            var book = TestDataBuilder.CreateBook();
            var userBook = TestDataBuilder.CreateUserBook(userId: lender.Id, bookId: book.Id, user: lender, book: book);
            var share = TestDataBuilder.CreateShare(
                userBookId: userBook.Id,
                borrower: borrower.Id,
                status: ShareStatus.PickedUp,
                userBook: userBook,
                borrowerUser: borrower
            );

            // Act
            var result = _validator.ValidateStatusTransition(share, ShareStatus.Disputed, lender.Id);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void ValidateStatusTransition_ToDisputedFromReturned_ReturnsSuccess()
        {
            // Arrange
            var lender = TestDataBuilder.CreateUser(id: "lender-1");
            var borrower = TestDataBuilder.CreateUser(id: "borrower-1");
            var book = TestDataBuilder.CreateBook();
            var userBook = TestDataBuilder.CreateUserBook(userId: lender.Id, bookId: book.Id, user: lender, book: book);
            var share = TestDataBuilder.CreateShare(
                userBookId: userBook.Id,
                borrower: borrower.Id,
                status: ShareStatus.Returned,
                userBook: userBook,
                borrowerUser: borrower
            );

            // Act
            var result = _validator.ValidateStatusTransition(share, ShareStatus.Disputed, borrower.Id);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        #endregion
    }
}
