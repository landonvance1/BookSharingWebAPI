using BookSharingApp.Common;
using BookSharingApp.Services;
using BookSharingApp.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace BookSharingApp.Tests.Services
{
    public class ArchiveShareAsyncTests : IDisposable
    {
        private readonly Mock<ILogger<ShareService>> _loggerMock;
        private readonly Mock<INotificationService> _notificationServiceMock;

        public ArchiveShareAsyncTests()
        {
            _loggerMock = new Mock<ILogger<ShareService>>();
            _notificationServiceMock = new Mock<INotificationService>();
        }

        public void Dispose()
        {
            // Cleanup if needed
        }

        [Fact]
        public async Task ArchiveShareAsync_WithTerminalStatusHomeSafe_CreatesShareUserState()
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
                status: ShareStatus.HomeSafe,  // Terminal status
                userBook: userBook,
                borrowerUser: borrower
            );
            context.Shares.Add(share);
            await context.SaveChangesAsync();

            // Act
            await shareService.ArchiveShareAsync(share.Id, lender.Id);

            // Assert
            var shareUserState = await context.ShareUserStates
                .FirstOrDefaultAsync(sus => sus.ShareId == share.Id && sus.UserId == lender.Id);

            shareUserState.Should().NotBeNull();
            shareUserState!.IsArchived.Should().BeTrue();
            shareUserState.ArchivedAt.Should().NotBeNull();
            shareUserState.ArchivedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task ArchiveShareAsync_WithTerminalStatusDeclined_CreatesShareUserState()
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
                status: ShareStatus.Declined,  // Terminal status
                userBook: userBook,
                borrowerUser: borrower
            );
            context.Shares.Add(share);
            await context.SaveChangesAsync();

            // Act
            await shareService.ArchiveShareAsync(share.Id, borrower.Id);

            // Assert
            var shareUserState = await context.ShareUserStates
                .FirstOrDefaultAsync(sus => sus.ShareId == share.Id && sus.UserId == borrower.Id);

            shareUserState.Should().NotBeNull();
            shareUserState!.IsArchived.Should().BeTrue();
        }

        [Fact]
        public async Task ArchiveShareAsync_WithTerminalStatusDisputed_CreatesShareUserState()
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
                status: ShareStatus.Disputed,  // Terminal status
                userBook: userBook,
                borrowerUser: borrower
            );
            context.Shares.Add(share);
            await context.SaveChangesAsync();

            // Act
            await shareService.ArchiveShareAsync(share.Id, lender.Id);

            // Assert
            var shareUserState = await context.ShareUserStates
                .FirstOrDefaultAsync(sus => sus.ShareId == share.Id && sus.UserId == lender.Id);

            shareUserState.Should().NotBeNull();
            shareUserState!.IsArchived.Should().BeTrue();
        }

        [Fact]
        public async Task ArchiveShareAsync_WithNonTerminalStatus_ThrowsInvalidOperationException()
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
                status: ShareStatus.Requested,  // NOT a terminal status
                userBook: userBook,
                borrowerUser: borrower
            );
            context.Shares.Add(share);
            await context.SaveChangesAsync();

            // Act
            var act = async () => await shareService.ArchiveShareAsync(share.Id, lender.Id);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Can only archive shares in terminal status (Declined, Disputed, or HomeSafe)");
        }

        [Fact]
        public async Task ArchiveShareAsync_WhenNotLenderOrBorrower_ThrowsUnauthorizedAccessException()
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

            // Act
            var act = async () => await shareService.ArchiveShareAsync(share.Id, unauthorizedUser.Id);

            // Assert
            await act.Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Only the lender or borrower can archive the share");
        }

        [Fact]
        public async Task ArchiveShareAsync_WhenAlreadyArchived_ThrowsInvalidOperationException()
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

            // Archive the share first time
            await shareService.ArchiveShareAsync(share.Id, lender.Id);

            // Act - Try to archive again
            var act = async () => await shareService.ArchiveShareAsync(share.Id, lender.Id);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Share is already archived");
        }

        [Fact]
        public async Task ArchiveShareAsync_AllowsIndependentArchivingByLenderAndBorrower()
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

            // Act - Archive by lender first
            await shareService.ArchiveShareAsync(share.Id, lender.Id);

            // Then archive by borrower
            await shareService.ArchiveShareAsync(share.Id, borrower.Id);

            // Assert
            var lenderState = await context.ShareUserStates
                .FirstOrDefaultAsync(sus => sus.ShareId == share.Id && sus.UserId == lender.Id);
            var borrowerState = await context.ShareUserStates
                .FirstOrDefaultAsync(sus => sus.ShareId == share.Id && sus.UserId == borrower.Id);

            lenderState.Should().NotBeNull();
            lenderState!.IsArchived.Should().BeTrue();

            borrowerState.Should().NotBeNull();
            borrowerState!.IsArchived.Should().BeTrue();

            // Both should have independent archive states
            context.ShareUserStates.Count(sus => sus.ShareId == share.Id).Should().Be(2);
        }
    }
}
