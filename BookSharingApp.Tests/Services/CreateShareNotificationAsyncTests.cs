using BookSharingApp.Common;
using BookSharingApp.Services;
using BookSharingApp.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace BookSharingApp.Tests.Services
{
    public class CreateShareNotificationAsyncTests : IDisposable
    {
        private readonly Mock<ILogger<NotificationService>> _loggerMock;

        public CreateShareNotificationAsyncTests()
        {
            _loggerMock = new Mock<ILogger<NotificationService>>();
        }

        public void Dispose()
        {
            // Cleanup if needed
        }

        [Fact]
        public async Task WhenLenderCreatesNotification_BorrowerIsRecipient()
        {
            // Arrange
            using var context = DbContextHelper.CreateInMemoryContext();
            var notificationService = new NotificationService(context, _loggerMock.Object);

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
                userBook: userBook,
                borrowerUser: borrower
            );
            context.Shares.Add(share);
            await context.SaveChangesAsync();

            // Act
            await notificationService.CreateShareNotificationAsync(
                share.Id,
                NotificationType.ShareStatusChanged,
                "Test message",
                lender.Id
            );

            // Assert
            var notification = await context.Notifications.FirstOrDefaultAsync();
            notification.Should().NotBeNull();
            notification!.UserId.Should().Be(borrower.Id);
        }

        [Fact]
        public async Task WhenBorrowerCreatesNotification_LenderIsRecipient()
        {
            // Arrange
            using var context = DbContextHelper.CreateInMemoryContext();
            var notificationService = new NotificationService(context, _loggerMock.Object);

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
                userBook: userBook,
                borrowerUser: borrower
            );
            context.Shares.Add(share);
            await context.SaveChangesAsync();

            // Act
            await notificationService.CreateShareNotificationAsync(
                share.Id,
                NotificationType.ShareStatusChanged,
                "Test message",
                borrower.Id
            );

            // Assert
            var notification = await context.Notifications.FirstOrDefaultAsync();
            notification.Should().NotBeNull();
            notification!.UserId.Should().Be(lender.Id);
        }

        [Fact]
        public async Task WithValidRequest_SetsNotificationTypeCorrectly()
        {
            // Arrange
            using var context = DbContextHelper.CreateInMemoryContext();
            var notificationService = new NotificationService(context, _loggerMock.Object);

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
                userBook: userBook,
                borrowerUser: borrower
            );
            context.Shares.Add(share);
            await context.SaveChangesAsync();

            // Act
            await notificationService.CreateShareNotificationAsync(
                share.Id,
                NotificationType.ShareDueDateChanged,
                "Test message",
                lender.Id
            );

            // Assert
            var notification = await context.Notifications.FirstOrDefaultAsync();
            notification.Should().NotBeNull();
            notification!.NotificationType.Should().Be(NotificationType.ShareDueDateChanged);
        }

        [Fact]
        public async Task WithValidRequest_SetsMessageCorrectly()
        {
            // Arrange
            using var context = DbContextHelper.CreateInMemoryContext();
            var notificationService = new NotificationService(context, _loggerMock.Object);

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
                userBook: userBook,
                borrowerUser: borrower
            );
            context.Shares.Add(share);
            await context.SaveChangesAsync();

            var expectedMessage = "Custom test message";

            // Act
            await notificationService.CreateShareNotificationAsync(
                share.Id,
                NotificationType.ShareStatusChanged,
                expectedMessage,
                lender.Id
            );

            // Assert
            var notification = await context.Notifications.FirstOrDefaultAsync();
            notification.Should().NotBeNull();
            notification!.Message.Should().Be(expectedMessage);
        }

        [Fact]
        public async Task WithValidRequest_SetsShareIdCorrectly()
        {
            // Arrange
            using var context = DbContextHelper.CreateInMemoryContext();
            var notificationService = new NotificationService(context, _loggerMock.Object);

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
                userBook: userBook,
                borrowerUser: borrower
            );
            context.Shares.Add(share);
            await context.SaveChangesAsync();

            // Act
            await notificationService.CreateShareNotificationAsync(
                share.Id,
                NotificationType.ShareStatusChanged,
                "Test message",
                lender.Id
            );

            // Assert
            var notification = await context.Notifications.FirstOrDefaultAsync();
            notification.Should().NotBeNull();
            notification!.ShareId.Should().Be(share.Id);
        }

        [Fact]
        public async Task WithValidRequest_SetsCreatedByUserIdCorrectly()
        {
            // Arrange
            using var context = DbContextHelper.CreateInMemoryContext();
            var notificationService = new NotificationService(context, _loggerMock.Object);

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
                userBook: userBook,
                borrowerUser: borrower
            );
            context.Shares.Add(share);
            await context.SaveChangesAsync();

            // Act
            await notificationService.CreateShareNotificationAsync(
                share.Id,
                NotificationType.ShareStatusChanged,
                "Test message",
                lender.Id
            );

            // Assert
            var notification = await context.Notifications.FirstOrDefaultAsync();
            notification.Should().NotBeNull();
            notification!.CreatedByUserId.Should().Be(lender.Id);
        }

        [Fact]
        public async Task WithValidRequest_SetsCreatedAtToUtcNow()
        {
            // Arrange
            using var context = DbContextHelper.CreateInMemoryContext();
            var notificationService = new NotificationService(context, _loggerMock.Object);

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
                userBook: userBook,
                borrowerUser: borrower
            );
            context.Shares.Add(share);
            await context.SaveChangesAsync();

            var beforeTime = DateTime.UtcNow.AddSeconds(-1);

            // Act
            await notificationService.CreateShareNotificationAsync(
                share.Id,
                NotificationType.ShareStatusChanged,
                "Test message",
                lender.Id
            );

            var afterTime = DateTime.UtcNow.AddSeconds(1);

            // Assert
            var notification = await context.Notifications.FirstOrDefaultAsync();
            notification.Should().NotBeNull();
            notification!.CreatedAt.Should().BeAfter(beforeTime);
            notification.CreatedAt.Should().BeBefore(afterTime);
        }

        [Fact]
        public async Task WithValidRequest_SetsReadAtToNull()
        {
            // Arrange
            using var context = DbContextHelper.CreateInMemoryContext();
            var notificationService = new NotificationService(context, _loggerMock.Object);

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
                userBook: userBook,
                borrowerUser: borrower
            );
            context.Shares.Add(share);
            await context.SaveChangesAsync();

            // Act
            await notificationService.CreateShareNotificationAsync(
                share.Id,
                NotificationType.ShareStatusChanged,
                "Test message",
                lender.Id
            );

            // Assert
            var notification = await context.Notifications.FirstOrDefaultAsync();
            notification.Should().NotBeNull();
            notification!.ReadAt.Should().BeNull();
        }

        [Fact]
        public async Task WithValidRequest_PersistsNotificationToDatabase()
        {
            // Arrange
            using var context = DbContextHelper.CreateInMemoryContext();
            var notificationService = new NotificationService(context, _loggerMock.Object);

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
                userBook: userBook,
                borrowerUser: borrower
            );
            context.Shares.Add(share);
            await context.SaveChangesAsync();

            // Act
            await notificationService.CreateShareNotificationAsync(
                share.Id,
                NotificationType.ShareStatusChanged,
                "Test message",
                lender.Id
            );

            // Assert
            var notificationCount = await context.Notifications.CountAsync();
            notificationCount.Should().Be(1);
        }

        [Fact]
        public async Task WhenShareNotFound_ThrowsInvalidOperationException()
        {
            // Arrange
            using var context = DbContextHelper.CreateInMemoryContext();
            var notificationService = new NotificationService(context, _loggerMock.Object);

            var nonExistentShareId = 999;

            // Act
            var act = async () => await notificationService.CreateShareNotificationAsync(
                nonExistentShareId,
                NotificationType.ShareStatusChanged,
                "Test message",
                "user-1"
            );

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Share not found");
        }

        [Fact]
        public async Task WhenUserNotInvolvedInShare_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            using var context = DbContextHelper.CreateInMemoryContext();
            var notificationService = new NotificationService(context, _loggerMock.Object);

            var lender = TestDataBuilder.CreateUser(id: "lender-1");
            var borrower = TestDataBuilder.CreateUser(id: "borrower-1");
            var outsider = TestDataBuilder.CreateUser(id: "outsider-1");
            var book = TestDataBuilder.CreateBook();

            context.Users.AddRange(lender, borrower, outsider);
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
                userBook: userBook,
                borrowerUser: borrower
            );
            context.Shares.Add(share);
            await context.SaveChangesAsync();

            // Act
            var act = async () => await notificationService.CreateShareNotificationAsync(
                share.Id,
                NotificationType.ShareStatusChanged,
                "Test message",
                outsider.Id
            );

            // Assert
            await act.Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("User is not involved in this share");
        }
    }
}
