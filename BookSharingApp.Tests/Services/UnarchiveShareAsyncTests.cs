using BookSharingApp.Common;
using BookSharingApp.Services;
using BookSharingApp.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace BookSharingApp.Tests.Services
{
    public class UnarchiveShareAsyncTests : IDisposable
    {
        private readonly Mock<ILogger<ShareService>> _loggerMock;
        private readonly Mock<INotificationService> _notificationServiceMock;

        public UnarchiveShareAsyncTests()
        {
            _loggerMock = new Mock<ILogger<ShareService>>();
            _notificationServiceMock = new Mock<INotificationService>();
        }

        public void Dispose()
        {
            // Cleanup if needed
        }

        [Fact]
        public async Task UnarchiveShareAsync_WhenArchived_SetsIsArchivedToFalseAndClearsArchivedAt()
        {
            // Arrange
            using var context = DbContextHelper.CreateInMemoryContext();
            var shareService = new ShareService(context, _loggerMock.Object, _notificationServiceMock.Object);

            var lender = TestDataBuilder.CreateUser(id: "lender-1");
            var borrower = TestDataBuilder.CreateUser(id: "borrower-1");
            var book = TestDataBuilder.CreateBook();

            context.Users.AddRange(lender, borrower);
            context.Books.Add(book);
            await context.SaveChangesAsync();

            var userBook = TestDataBuilder.CreateUserBook(
                userId: lender.Id,
                bookId: book.Id,
                user: lender,
                book: book
            );
            context.UserBooks.Add(userBook);
            await context.SaveChangesAsync();

            var share = TestDataBuilder.CreateShare(
                userBookId: userBook.Id,
                borrower: borrower.Id,
                status: ShareStatus.HomeSafe,
                userBook: userBook,
                borrowerUser: borrower
            );
            context.Shares.Add(share);
            await context.SaveChangesAsync();

            // Archive the share first
            var shareUserState = TestDataBuilder.CreateShareUserState(
                shareId: share.Id,
                userId: lender.Id,
                isArchived: true,
                archivedAt: DateTime.UtcNow,
                share: share,
                user: lender
            );
            context.ShareUserStates.Add(shareUserState);
            await context.SaveChangesAsync();

            // Act
            await shareService.UnarchiveShareAsync(share.Id, lender.Id);

            // Assert
            var updatedState = await context.ShareUserStates
                .FirstOrDefaultAsync(sus => sus.ShareId == share.Id && sus.UserId == lender.Id);

            updatedState.Should().NotBeNull();
            updatedState!.IsArchived.Should().BeFalse();
            updatedState.ArchivedAt.Should().BeNull();
        }

        [Fact]
        public async Task UnarchiveShareAsync_WhenNotArchived_ThrowsInvalidOperationException()
        {
            // Arrange
            using var context = DbContextHelper.CreateInMemoryContext();
            var shareService = new ShareService(context, _loggerMock.Object, _notificationServiceMock.Object);

            var lender = TestDataBuilder.CreateUser(id: "lender-1");
            var borrower = TestDataBuilder.CreateUser(id: "borrower-1");
            var book = TestDataBuilder.CreateBook();

            context.Users.AddRange(lender, borrower);
            context.Books.Add(book);
            await context.SaveChangesAsync();

            var userBook = TestDataBuilder.CreateUserBook(
                userId: lender.Id,
                bookId: book.Id,
                user: lender,
                book: book
            );
            context.UserBooks.Add(userBook);
            await context.SaveChangesAsync();

            var share = TestDataBuilder.CreateShare(
                userBookId: userBook.Id,
                borrower: borrower.Id,
                status: ShareStatus.HomeSafe,
                userBook: userBook,
                borrowerUser: borrower
            );
            context.Shares.Add(share);
            await context.SaveChangesAsync();

            // Act - Try to unarchive without archiving first
            var act = async () => await shareService.UnarchiveShareAsync(share.Id, lender.Id);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Share is not archived");
        }

        [Fact]
        public async Task UnarchiveShareAsync_WhenNotLenderOrBorrower_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            using var context = DbContextHelper.CreateInMemoryContext();
            var shareService = new ShareService(context, _loggerMock.Object, _notificationServiceMock.Object);

            var lender = TestDataBuilder.CreateUser(id: "lender-1");
            var borrower = TestDataBuilder.CreateUser(id: "borrower-1");
            var unauthorizedUser = TestDataBuilder.CreateUser(id: "unauthorized-user");
            var book = TestDataBuilder.CreateBook();

            context.Users.AddRange(lender, borrower, unauthorizedUser);
            context.Books.Add(book);
            await context.SaveChangesAsync();

            var userBook = TestDataBuilder.CreateUserBook(
                userId: lender.Id,
                bookId: book.Id,
                user: lender,
                book: book
            );
            context.UserBooks.Add(userBook);
            await context.SaveChangesAsync();

            var share = TestDataBuilder.CreateShare(
                userBookId: userBook.Id,
                borrower: borrower.Id,
                status: ShareStatus.HomeSafe,
                userBook: userBook,
                borrowerUser: borrower
            );
            context.Shares.Add(share);
            await context.SaveChangesAsync();

            // Archive the share
            var shareUserState = TestDataBuilder.CreateShareUserState(
                shareId: share.Id,
                userId: lender.Id,
                isArchived: true,
                archivedAt: DateTime.UtcNow,
                share: share,
                user: lender
            );
            context.ShareUserStates.Add(shareUserState);
            await context.SaveChangesAsync();

            // Act - Try to unarchive as unauthorized user
            var act = async () => await shareService.UnarchiveShareAsync(share.Id, unauthorizedUser.Id);

            // Assert
            await act.Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Only the lender or borrower can unarchive the share");
        }
    }
}
