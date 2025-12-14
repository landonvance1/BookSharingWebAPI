using BookSharingApp.Common;
using BookSharingApp.Services;
using BookSharingApp.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace BookSharingApp.Tests.Services
{
    public class GetUnreadNotificationsAsyncTests : IDisposable
    {
        private readonly Mock<ILogger<NotificationService>> _loggerMock;

        public GetUnreadNotificationsAsyncTests()
        {
            _loggerMock = new Mock<ILogger<NotificationService>>();
        }

        public void Dispose()
        {
            // Cleanup if needed
        }

        [Fact]
        public async Task WithUnreadNotifications_ReturnsOnlyUnreadNotifications()
        {
            // Arrange
            using var context = DbContextHelper.CreateInMemoryContext();
            var notificationService = new NotificationService(context, _loggerMock.Object);

            var user = TestDataBuilder.CreateUser(id: "user-1");
            var creator = TestDataBuilder.CreateUser(id: "creator-1");
            context.Users.AddRange(user, creator);
            await context.SaveChangesAsync();

            var unreadNotification = TestDataBuilder.CreateNotification(
                userId: user.Id,
                createdByUserId: creator.Id,
                readAt: null
            );

            // Link navigation properties to tracked entities
            unreadNotification.User = user;
            unreadNotification.CreatedByUser = creator;

            context.Notifications.Add(unreadNotification);
            await context.SaveChangesAsync();

            // Act
            var result = await notificationService.GetUnreadNotificationsAsync(user.Id);

            // Assert
            result.Should().HaveCount(1);
            result[0].Id.Should().Be(unreadNotification.Id);
        }

        [Fact]
        public async Task WithReadAndUnreadNotifications_ExcludesReadNotifications()
        {
            // Arrange
            using var context = DbContextHelper.CreateInMemoryContext();
            var notificationService = new NotificationService(context, _loggerMock.Object);

            var user = TestDataBuilder.CreateUser(id: "user-1");
            var creator = TestDataBuilder.CreateUser(id: "creator-1");
            context.Users.AddRange(user, creator);
            await context.SaveChangesAsync();

            var unreadNotification = TestDataBuilder.CreateNotification(
                userId: user.Id,
                createdByUserId: creator.Id,
                readAt: null
            );
            unreadNotification.User = user;
            unreadNotification.CreatedByUser = creator;

            var readNotification = TestDataBuilder.CreateNotification(
                userId: user.Id,
                createdByUserId: creator.Id,
                readAt: DateTime.UtcNow.AddHours(-1)
            );
            readNotification.User = user;
            readNotification.CreatedByUser = creator;

            context.Notifications.AddRange(unreadNotification, readNotification);
            await context.SaveChangesAsync();

            // Act
            var result = await notificationService.GetUnreadNotificationsAsync(user.Id);

            // Assert
            result.Should().HaveCount(1);
            result[0].Id.Should().Be(unreadNotification.Id);
            result.Should().NotContain(n => n.Id == readNotification.Id);
        }

        [Fact]
        public async Task WithNoUnreadNotifications_ReturnsEmptyList()
        {
            // Arrange
            using var context = DbContextHelper.CreateInMemoryContext();
            var notificationService = new NotificationService(context, _loggerMock.Object);

            var user = TestDataBuilder.CreateUser(id: "user-1");
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var readNotification = TestDataBuilder.CreateNotification(
                userId: user.Id,
                readAt: DateTime.UtcNow
            );
            context.Notifications.Add(readNotification);
            await context.SaveChangesAsync();

            // Act
            var result = await notificationService.GetUnreadNotificationsAsync(user.Id);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task WithMultipleUsers_ReturnsOnlySpecifiedUserNotifications()
        {
            // Arrange
            using var context = DbContextHelper.CreateInMemoryContext();
            var notificationService = new NotificationService(context, _loggerMock.Object);

            var user1 = TestDataBuilder.CreateUser(id: "user-1");
            var user2 = TestDataBuilder.CreateUser(id: "user-2");
            var creator = TestDataBuilder.CreateUser(id: "creator-1");
            context.Users.AddRange(user1, user2, creator);
            await context.SaveChangesAsync();

            var user1Notification = TestDataBuilder.CreateNotification(
                userId: user1.Id,
                createdByUserId: creator.Id,
                readAt: null
            );
            user1Notification.User = user1;
            user1Notification.CreatedByUser = creator;

            var user2Notification = TestDataBuilder.CreateNotification(
                userId: user2.Id,
                createdByUserId: creator.Id,
                readAt: null
            );
            user2Notification.User = user2;
            user2Notification.CreatedByUser = creator;

            context.Notifications.AddRange(user1Notification, user2Notification);
            await context.SaveChangesAsync();

            // Act
            var result = await notificationService.GetUnreadNotificationsAsync(user1.Id);

            // Assert
            result.Should().HaveCount(1);
            result[0].Id.Should().Be(user1Notification.Id);
            result.Should().NotContain(n => n.Id == user2Notification.Id);
        }

        [Fact]
        public async Task WithMultipleNotifications_ReturnsOrderedByCreatedAtDescending()
        {
            // Arrange
            using var context = DbContextHelper.CreateInMemoryContext();
            var notificationService = new NotificationService(context, _loggerMock.Object);

            var user = TestDataBuilder.CreateUser(id: "user-1");
            var creator = TestDataBuilder.CreateUser(id: "creator-1");
            context.Users.AddRange(user, creator);
            await context.SaveChangesAsync();

            var oldestNotification = TestDataBuilder.CreateNotification(
                userId: user.Id,
                createdByUserId: creator.Id,
                createdAt: DateTime.UtcNow.AddHours(-3),
                readAt: null
            );
            oldestNotification.User = user;
            oldestNotification.CreatedByUser = creator;

            var middleNotification = TestDataBuilder.CreateNotification(
                userId: user.Id,
                createdByUserId: creator.Id,
                createdAt: DateTime.UtcNow.AddHours(-2),
                readAt: null
            );
            middleNotification.User = user;
            middleNotification.CreatedByUser = creator;

            var newestNotification = TestDataBuilder.CreateNotification(
                userId: user.Id,
                createdByUserId: creator.Id,
                createdAt: DateTime.UtcNow.AddHours(-1),
                readAt: null
            );
            newestNotification.User = user;
            newestNotification.CreatedByUser = creator;

            context.Notifications.AddRange(oldestNotification, middleNotification, newestNotification);
            await context.SaveChangesAsync();

            // Act
            var result = await notificationService.GetUnreadNotificationsAsync(user.Id);

            // Assert
            result.Should().HaveCount(3);
            result[0].Id.Should().Be(newestNotification.Id);
            result[1].Id.Should().Be(middleNotification.Id);
            result[2].Id.Should().Be(oldestNotification.Id);
        }

        [Fact]
        public async Task WithValidRequest_IncludesShareNavigationProperty()
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

            var notification = TestDataBuilder.CreateNotification(
                userId: borrower.Id,
                createdByUserId: lender.Id,
                shareId: share.Id,
                readAt: null
            );
            notification.User = borrower;
            notification.CreatedByUser = lender;
            notification.Share = share;

            context.Notifications.Add(notification);
            await context.SaveChangesAsync();

            // Act
            var result = await notificationService.GetUnreadNotificationsAsync(borrower.Id);

            // Assert
            result.Should().HaveCount(1);
            result[0].Share.Should().NotBeNull();
            result[0].Share!.Id.Should().Be(share.Id);
        }

        [Fact]
        public async Task WithValidRequest_IncludesUserBookNavigationProperty()
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

            var notification = TestDataBuilder.CreateNotification(
                userId: borrower.Id,
                createdByUserId: lender.Id,
                shareId: share.Id,
                readAt: null
            );
            notification.User = borrower;
            notification.CreatedByUser = lender;
            notification.Share = share;

            context.Notifications.Add(notification);
            await context.SaveChangesAsync();

            // Act
            var result = await notificationService.GetUnreadNotificationsAsync(borrower.Id);

            // Assert
            result.Should().HaveCount(1);
            result[0].Share.Should().NotBeNull();
            result[0].Share!.UserBook.Should().NotBeNull();
            result[0].Share?.UserBook.Id.Should().Be(userBook.Id);
        }

        [Fact]
        public async Task WithValidRequest_IncludesBookNavigationProperty()
        {
            // Arrange
            using var context = DbContextHelper.CreateInMemoryContext();
            var notificationService = new NotificationService(context, _loggerMock.Object);

            var lender = TestDataBuilder.CreateUser(id: "lender-1");
            var borrower = TestDataBuilder.CreateUser(id: "borrower-1");
            var book = TestDataBuilder.CreateBook(title: "Test Book Title");

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

            var notification = TestDataBuilder.CreateNotification(
                userId: borrower.Id,
                createdByUserId: lender.Id,
                shareId: share.Id,
                readAt: null
            );
            notification.User = borrower;
            notification.CreatedByUser = lender;
            notification.Share = share;

            context.Notifications.Add(notification);
            await context.SaveChangesAsync();

            // Act
            var result = await notificationService.GetUnreadNotificationsAsync(borrower.Id);

            // Assert
            result.Should().HaveCount(1);
            result[0].Share.Should().NotBeNull();
            result[0].Share!.UserBook.Should().NotBeNull();
            result[0].Share?.UserBook.Book.Should().NotBeNull();
            result[0].Share?.UserBook.Book.Title.Should().Be("Test Book Title");
        }

        [Fact]
        public async Task WithValidRequest_IncludesCreatedByUserNavigationProperty()
        {
            // Arrange
            using var context = DbContextHelper.CreateInMemoryContext();
            var notificationService = new NotificationService(context, _loggerMock.Object);

            var lender = TestDataBuilder.CreateUser(id: "lender-1", firstName: "John");
            var borrower = TestDataBuilder.CreateUser(id: "borrower-1");

            context.Users.AddRange(lender, borrower);
            await context.SaveChangesAsync();

            var notification = TestDataBuilder.CreateNotification(
                userId: borrower.Id,
                createdByUserId: lender.Id,
                readAt: null
            );
            context.Notifications.Add(notification);
            await context.SaveChangesAsync();

            // Act
            var result = await notificationService.GetUnreadNotificationsAsync(borrower.Id);

            // Assert
            result.Should().HaveCount(1);
            result[0].CreatedByUser.Should().NotBeNull();
            result[0].CreatedByUser.Id.Should().Be(lender.Id);
            result[0].CreatedByUser.FirstName.Should().Be("John");
        }

        [Fact]
        public async Task WithMixedNotificationTypes_ReturnsAllUnreadTypes()
        {
            // Arrange
            using var context = DbContextHelper.CreateInMemoryContext();
            var notificationService = new NotificationService(context, _loggerMock.Object);

            var user = TestDataBuilder.CreateUser(id: "user-1");
            var creator = TestDataBuilder.CreateUser(id: "creator-1");
            context.Users.AddRange(user, creator);
            await context.SaveChangesAsync();

            var statusNotification = TestDataBuilder.CreateNotification(
                userId: user.Id,
                createdByUserId: creator.Id,
                notificationType: NotificationType.ShareStatusChanged,
                readAt: null
            );
            statusNotification.User = user;
            statusNotification.CreatedByUser = creator;

            var dueDateNotification = TestDataBuilder.CreateNotification(
                userId: user.Id,
                createdByUserId: creator.Id,
                notificationType: NotificationType.ShareDueDateChanged,
                readAt: null
            );
            dueDateNotification.User = user;
            dueDateNotification.CreatedByUser = creator;

            var messageNotification = TestDataBuilder.CreateNotification(
                userId: user.Id,
                createdByUserId: creator.Id,
                notificationType: NotificationType.ShareMessageReceived,
                readAt: null
            );
            messageNotification.User = user;
            messageNotification.CreatedByUser = creator;

            context.Notifications.AddRange(statusNotification, dueDateNotification, messageNotification);
            await context.SaveChangesAsync();

            // Act
            var result = await notificationService.GetUnreadNotificationsAsync(user.Id);

            // Assert
            result.Should().HaveCount(3);
            result.Should().Contain(n => n.NotificationType == NotificationType.ShareStatusChanged);
            result.Should().Contain(n => n.NotificationType == NotificationType.ShareDueDateChanged);
            result.Should().Contain(n => n.NotificationType == NotificationType.ShareMessageReceived);
        }
    }
}
