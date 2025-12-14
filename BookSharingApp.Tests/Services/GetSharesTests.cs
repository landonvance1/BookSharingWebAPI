using BookSharingApp.Common;
using BookSharingApp.Services;
using BookSharingApp.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace BookSharingApp.Tests.Services
{
    public class GetSharesTests : IDisposable
    {
        private readonly Mock<ILogger<ShareService>> _loggerMock;
        private readonly Mock<INotificationService> _notificationServiceMock;

        public GetSharesTests()
        {
            _loggerMock = new Mock<ILogger<ShareService>>();
            _notificationServiceMock = new Mock<INotificationService>();
        }

        public void Dispose()
        {
            // Cleanup if needed
        }

        [Fact]
        public async Task GetBorrowerSharesAsync_ExcludesArchivedShares()
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

            // Create active share
            var activeShare = TestDataBuilder.CreateShare(
                userBookId: userBook.Id,
                borrower: borrower.Id,
                status: ShareStatus.Requested,
                userBook: userBook,
                borrowerUser: borrower
            );

            // Create archived share
            var archivedShare = TestDataBuilder.CreateShare(
                userBookId: userBook.Id,
                borrower: borrower.Id,
                status: ShareStatus.HomeSafe,
                userBook: userBook,
                borrowerUser: borrower
            );

            context.Shares.AddRange(activeShare, archivedShare);
            await context.SaveChangesAsync();

            // Archive the second share
            var shareUserState = TestDataBuilder.CreateShareUserState(
                shareId: archivedShare.Id,
                userId: borrower.Id,
                isArchived: true,
                archivedAt: DateTime.UtcNow,
                share: archivedShare,
                user: borrower
            );
            context.ShareUserStates.Add(shareUserState);
            await context.SaveChangesAsync();

            // Act
            var result = await shareService.GetBorrowerSharesAsync(borrower.Id);

            // Assert
            result.Should().HaveCount(1);
            result.Should().Contain(s => s.Id == activeShare.Id);
            result.Should().NotContain(s => s.Id == archivedShare.Id);
        }

        [Fact]
        public async Task GetBorrowerSharesAsync_IncludesNavigationProperties()
        {
            // Arrange
            using var context = DbContextHelper.CreateInMemoryContext();
            var shareService = new ShareService(context, _loggerMock.Object, _notificationServiceMock.Object);

            var lender = TestDataBuilder.CreateUser(id: "lender-1", firstName: "John", lastName: "Doe");
            var borrower = TestDataBuilder.CreateUser(id: "borrower-1");
            var book = TestDataBuilder.CreateBook(title: "Test Book", author: "Test Author");

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
                status: ShareStatus.Ready,
                userBook: userBook,
                borrowerUser: borrower
            );
            context.Shares.Add(share);
            await context.SaveChangesAsync();

            // Act
            var result = await shareService.GetBorrowerSharesAsync(borrower.Id);

            // Assert
            result.Should().HaveCount(1);
            var retrievedShare = result.First();
            retrievedShare.UserBook.Should().NotBeNull();
            retrievedShare.UserBook.Book.Should().NotBeNull();
            retrievedShare.UserBook.Book.Title.Should().Be("Test Book");
            retrievedShare.UserBook.User.Should().NotBeNull();
            retrievedShare.UserBook.User.FirstName.Should().Be("John");
        }

        [Fact]
        public async Task GetLenderSharesAsync_ExcludesArchivedShares()
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

            // Create active share
            var activeShare = TestDataBuilder.CreateShare(
                userBookId: userBook.Id,
                borrower: borrower.Id,
                status: ShareStatus.PickedUp,
                userBook: userBook,
                borrowerUser: borrower
            );

            // Create archived share
            var archivedShare = TestDataBuilder.CreateShare(
                userBookId: userBook.Id,
                borrower: borrower.Id,
                status: ShareStatus.Declined,
                userBook: userBook,
                borrowerUser: borrower
            );

            context.Shares.AddRange(activeShare, archivedShare);
            await context.SaveChangesAsync();

            // Archive the second share for the lender
            var shareUserState = TestDataBuilder.CreateShareUserState(
                shareId: archivedShare.Id,
                userId: lender.Id,
                isArchived: true,
                archivedAt: DateTime.UtcNow,
                share: archivedShare,
                user: lender
            );
            context.ShareUserStates.Add(shareUserState);
            await context.SaveChangesAsync();

            // Act
            var result = await shareService.GetLenderSharesAsync(lender.Id);

            // Assert
            result.Should().HaveCount(1);
            result.Should().Contain(s => s.Id == activeShare.Id);
            result.Should().NotContain(s => s.Id == archivedShare.Id);
        }

        [Fact]
        public async Task GetLenderSharesAsync_IncludesNavigationProperties()
        {
            // Arrange
            using var context = DbContextHelper.CreateInMemoryContext();
            var shareService = new ShareService(context, _loggerMock.Object, _notificationServiceMock.Object);

            var lender = TestDataBuilder.CreateUser(id: "lender-1");
            var borrower = TestDataBuilder.CreateUser(id: "borrower-1", firstName: "Jane", lastName: "Smith");
            var book = TestDataBuilder.CreateBook(title: "The Great Book", author: "Famous Author");

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
                status: ShareStatus.Returned,
                userBook: userBook,
                borrowerUser: borrower
            );
            context.Shares.Add(share);
            await context.SaveChangesAsync();

            // Act
            var result = await shareService.GetLenderSharesAsync(lender.Id);

            // Assert
            result.Should().HaveCount(1);
            var retrievedShare = result.First();
            retrievedShare.UserBook.Should().NotBeNull();
            retrievedShare.UserBook.Book.Should().NotBeNull();
            retrievedShare.UserBook.Book.Title.Should().Be("The Great Book");
            retrievedShare.BorrowerUser.Should().NotBeNull();
            retrievedShare.BorrowerUser.FirstName.Should().Be("Jane");
        }

        [Fact]
        public async Task GetArchivedBorrowerSharesAsync_ReturnsOnlyArchivedShares()
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

            // Create active share
            var activeShare = TestDataBuilder.CreateShare(
                userBookId: userBook.Id,
                borrower: borrower.Id,
                status: ShareStatus.Requested,
                userBook: userBook,
                borrowerUser: borrower
            );

            // Create archived share
            var archivedShare = TestDataBuilder.CreateShare(
                userBookId: userBook.Id,
                borrower: borrower.Id,
                status: ShareStatus.HomeSafe,
                userBook: userBook,
                borrowerUser: borrower
            );

            context.Shares.AddRange(activeShare, archivedShare);
            await context.SaveChangesAsync();

            // Archive the second share
            var shareUserState = TestDataBuilder.CreateShareUserState(
                shareId: archivedShare.Id,
                userId: borrower.Id,
                isArchived: true,
                archivedAt: DateTime.UtcNow,
                share: archivedShare,
                user: borrower
            );
            context.ShareUserStates.Add(shareUserState);
            await context.SaveChangesAsync();

            // Act
            var result = await shareService.GetArchivedBorrowerSharesAsync(borrower.Id);

            // Assert
            result.Should().HaveCount(1);
            result.Should().Contain(s => s.Id == archivedShare.Id);
            result.Should().NotContain(s => s.Id == activeShare.Id);
        }

        [Fact]
        public async Task GetArchivedBorrowerSharesAsync_IncludesNavigationProperties()
        {
            // Arrange
            using var context = DbContextHelper.CreateInMemoryContext();
            var shareService = new ShareService(context, _loggerMock.Object, _notificationServiceMock.Object);

            var lender = TestDataBuilder.CreateUser(id: "lender-1", firstName: "Bob", lastName: "Builder");
            var borrower = TestDataBuilder.CreateUser(id: "borrower-1");
            var book = TestDataBuilder.CreateBook(title: "Archived Book", author: "Past Author");

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
                status: ShareStatus.Disputed,
                userBook: userBook,
                borrowerUser: borrower
            );
            context.Shares.Add(share);
            await context.SaveChangesAsync();

            var shareUserState = TestDataBuilder.CreateShareUserState(
                shareId: share.Id,
                userId: borrower.Id,
                isArchived: true,
                archivedAt: DateTime.UtcNow,
                share: share,
                user: borrower
            );
            context.ShareUserStates.Add(shareUserState);
            await context.SaveChangesAsync();

            // Act
            var result = await shareService.GetArchivedBorrowerSharesAsync(borrower.Id);

            // Assert
            result.Should().HaveCount(1);
            var retrievedShare = result.First();
            retrievedShare.UserBook.Should().NotBeNull();
            retrievedShare.UserBook.Book.Should().NotBeNull();
            retrievedShare.UserBook.Book.Title.Should().Be("Archived Book");
            retrievedShare.UserBook.User.Should().NotBeNull();
            retrievedShare.UserBook.User.LastName.Should().Be("Builder");
        }

        [Fact]
        public async Task GetArchivedLenderSharesAsync_ReturnsOnlyArchivedShares()
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

            // Create active share
            var activeShare = TestDataBuilder.CreateShare(
                userBookId: userBook.Id,
                borrower: borrower.Id,
                status: ShareStatus.Ready,
                userBook: userBook,
                borrowerUser: borrower
            );

            // Create archived share
            var archivedShare = TestDataBuilder.CreateShare(
                userBookId: userBook.Id,
                borrower: borrower.Id,
                status: ShareStatus.Declined,
                userBook: userBook,
                borrowerUser: borrower
            );

            context.Shares.AddRange(activeShare, archivedShare);
            await context.SaveChangesAsync();

            // Archive the second share for the lender
            var shareUserState = TestDataBuilder.CreateShareUserState(
                shareId: archivedShare.Id,
                userId: lender.Id,
                isArchived: true,
                archivedAt: DateTime.UtcNow,
                share: archivedShare,
                user: lender
            );
            context.ShareUserStates.Add(shareUserState);
            await context.SaveChangesAsync();

            // Act
            var result = await shareService.GetArchivedLenderSharesAsync(lender.Id);

            // Assert
            result.Should().HaveCount(1);
            result.Should().Contain(s => s.Id == archivedShare.Id);
            result.Should().NotContain(s => s.Id == activeShare.Id);
        }

        [Fact]
        public async Task GetArchivedLenderSharesAsync_IncludesNavigationProperties()
        {
            // Arrange
            using var context = DbContextHelper.CreateInMemoryContext();
            var shareService = new ShareService(context, _loggerMock.Object, _notificationServiceMock.Object);

            var lender = TestDataBuilder.CreateUser(id: "lender-1");
            var borrower = TestDataBuilder.CreateUser(id: "borrower-1", firstName: "Alice", lastName: "Wonder");
            var book = TestDataBuilder.CreateBook(title: "Old Book", author: "Classic Author");

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
            var result = await shareService.GetArchivedLenderSharesAsync(lender.Id);

            // Assert
            result.Should().HaveCount(1);
            var retrievedShare = result.First();
            retrievedShare.UserBook.Should().NotBeNull();
            retrievedShare.UserBook.Book.Should().NotBeNull();
            retrievedShare.UserBook.Book.Title.Should().Be("Old Book");
            retrievedShare.BorrowerUser.Should().NotBeNull();
            retrievedShare.BorrowerUser.FirstName.Should().Be("Alice");
        }
    }
}
