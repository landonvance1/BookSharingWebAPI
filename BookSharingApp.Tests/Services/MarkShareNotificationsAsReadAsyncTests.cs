using BookSharingApp.Common;
using BookSharingApp.Services;
using BookSharingApp.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace BookSharingApp.Tests.Services
{
    public class MarkShareNotificationsAsReadAsyncTests : IDisposable
    {
        private readonly Mock<ILogger<NotificationService>> _loggerMock;

        public MarkShareNotificationsAsReadAsyncTests()
        {
            _loggerMock = new Mock<ILogger<NotificationService>>();
        }

        public void Dispose()
        {
            // Cleanup if needed
        }

        [Fact]
        public async Task WithUnreadShareStatusChangedNotification_MarksAsRead()
        {
            // Arrange
            using var context = DbContextHelper.CreateInMemoryContext();
            var notificationService = new NotificationService(context, _loggerMock.Object);

            var user = TestDataBuilder.CreateUser(id: "user-1");
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var notification = TestDataBuilder.CreateNotification(
                userId: user.Id,
                notificationType: NotificationType.ShareStatusChanged,
                shareId: 1,
                readAt: null
            );
            context.Notifications.Add(notification);
            await context.SaveChangesAsync();

            // Act
            await notificationService.MarkShareNotificationsAsReadAsync(1, user.Id);

            // Assert
            var updatedNotification = await context.Notifications.FindAsync(notification.Id);
            updatedNotification.Should().NotBeNull();
            updatedNotification!.ReadAt.Should().NotBeNull();
        }

        [Fact]
        public async Task WithUnreadShareDueDateChangedNotification_MarksAsRead()
        {
            // Arrange
            using var context = DbContextHelper.CreateInMemoryContext();
            var notificationService = new NotificationService(context, _loggerMock.Object);

            var user = TestDataBuilder.CreateUser(id: "user-1");
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var notification = TestDataBuilder.CreateNotification(
                userId: user.Id,
                notificationType: NotificationType.ShareDueDateChanged,
                shareId: 1,
                readAt: null
            );
            context.Notifications.Add(notification);
            await context.SaveChangesAsync();

            // Act
            await notificationService.MarkShareNotificationsAsReadAsync(1, user.Id);

            // Assert
            var updatedNotification = await context.Notifications.FindAsync(notification.Id);
            updatedNotification.Should().NotBeNull();
            updatedNotification!.ReadAt.Should().NotBeNull();
        }

        [Fact]
        public async Task WithUnreadShareMessageReceivedNotification_DoesNotMarkAsRead()
        {
            // Arrange
            using var context = DbContextHelper.CreateInMemoryContext();
            var notificationService = new NotificationService(context, _loggerMock.Object);

            var user = TestDataBuilder.CreateUser(id: "user-1");
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var notification = TestDataBuilder.CreateNotification(
                userId: user.Id,
                notificationType: NotificationType.ShareMessageReceived,
                shareId: 1,
                readAt: null
            );
            context.Notifications.Add(notification);
            await context.SaveChangesAsync();

            // Act
            await notificationService.MarkShareNotificationsAsReadAsync(1, user.Id);

            // Assert
            var updatedNotification = await context.Notifications.FindAsync(notification.Id);
            updatedNotification.Should().NotBeNull();
            updatedNotification!.ReadAt.Should().BeNull();
        }

        [Fact]
        public async Task WithMixedNotificationTypes_OnlyMarksShareStatusAndDueDateTypes()
        {
            // Arrange
            using var context = DbContextHelper.CreateInMemoryContext();
            var notificationService = new NotificationService(context, _loggerMock.Object);

            var user = TestDataBuilder.CreateUser(id: "user-1");
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var statusNotification = TestDataBuilder.CreateNotification(
                userId: user.Id,
                notificationType: NotificationType.ShareStatusChanged,
                shareId: 1,
                readAt: null
            );
            var dueDateNotification = TestDataBuilder.CreateNotification(
                userId: user.Id,
                notificationType: NotificationType.ShareDueDateChanged,
                shareId: 1,
                readAt: null
            );
            var messageNotification = TestDataBuilder.CreateNotification(
                userId: user.Id,
                notificationType: NotificationType.ShareMessageReceived,
                shareId: 1,
                readAt: null
            );
            context.Notifications.AddRange(statusNotification, dueDateNotification, messageNotification);
            await context.SaveChangesAsync();

            // Act
            await notificationService.MarkShareNotificationsAsReadAsync(1, user.Id);

            // Assert
            var updatedStatusNotification = await context.Notifications.FindAsync(statusNotification.Id);
            var updatedDueDateNotification = await context.Notifications.FindAsync(dueDateNotification.Id);
            var updatedMessageNotification = await context.Notifications.FindAsync(messageNotification.Id);

            updatedStatusNotification!.ReadAt.Should().NotBeNull();
            updatedDueDateNotification!.ReadAt.Should().NotBeNull();
            updatedMessageNotification!.ReadAt.Should().BeNull();
        }

        [Fact]
        public async Task WithNotificationsForDifferentUser_DoesNotMarkAsRead()
        {
            // Arrange
            using var context = DbContextHelper.CreateInMemoryContext();
            var notificationService = new NotificationService(context, _loggerMock.Object);

            var user1 = TestDataBuilder.CreateUser(id: "user-1");
            var user2 = TestDataBuilder.CreateUser(id: "user-2");
            context.Users.AddRange(user1, user2);
            await context.SaveChangesAsync();

            var user1Notification = TestDataBuilder.CreateNotification(
                userId: user1.Id,
                notificationType: NotificationType.ShareStatusChanged,
                shareId: 1,
                readAt: null
            );
            var user2Notification = TestDataBuilder.CreateNotification(
                userId: user2.Id,
                notificationType: NotificationType.ShareStatusChanged,
                shareId: 1,
                readAt: null
            );
            context.Notifications.AddRange(user1Notification, user2Notification);
            await context.SaveChangesAsync();

            // Act
            await notificationService.MarkShareNotificationsAsReadAsync(1, user1.Id);

            // Assert
            var updatedUser1Notification = await context.Notifications.FindAsync(user1Notification.Id);
            var updatedUser2Notification = await context.Notifications.FindAsync(user2Notification.Id);

            updatedUser1Notification!.ReadAt.Should().NotBeNull();
            updatedUser2Notification!.ReadAt.Should().BeNull();
        }

        [Fact]
        public async Task WithNotificationsForDifferentShare_DoesNotMarkAsRead()
        {
            // Arrange
            using var context = DbContextHelper.CreateInMemoryContext();
            var notificationService = new NotificationService(context, _loggerMock.Object);

            var user = TestDataBuilder.CreateUser(id: "user-1");
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var share1Notification = TestDataBuilder.CreateNotification(
                userId: user.Id,
                notificationType: NotificationType.ShareStatusChanged,
                shareId: 1,
                readAt: null
            );
            var share2Notification = TestDataBuilder.CreateNotification(
                userId: user.Id,
                notificationType: NotificationType.ShareStatusChanged,
                shareId: 2,
                readAt: null
            );
            context.Notifications.AddRange(share1Notification, share2Notification);
            await context.SaveChangesAsync();

            // Act
            await notificationService.MarkShareNotificationsAsReadAsync(1, user.Id);

            // Assert
            var updatedShare1Notification = await context.Notifications.FindAsync(share1Notification.Id);
            var updatedShare2Notification = await context.Notifications.FindAsync(share2Notification.Id);

            updatedShare1Notification!.ReadAt.Should().NotBeNull();
            updatedShare2Notification!.ReadAt.Should().BeNull();
        }

        [Fact]
        public async Task WithReadShareNotifications_DoesNotUpdateReadAt()
        {
            // Arrange
            using var context = DbContextHelper.CreateInMemoryContext();
            var notificationService = new NotificationService(context, _loggerMock.Object);

            var user = TestDataBuilder.CreateUser(id: "user-1");
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var originalReadTime = DateTime.UtcNow.AddHours(-2);
            var notification = TestDataBuilder.CreateNotification(
                userId: user.Id,
                notificationType: NotificationType.ShareStatusChanged,
                shareId: 1,
                readAt: originalReadTime
            );
            context.Notifications.Add(notification);
            await context.SaveChangesAsync();

            // Act
            await notificationService.MarkShareNotificationsAsReadAsync(1, user.Id);

            // Assert
            var updatedNotification = await context.Notifications.FindAsync(notification.Id);
            updatedNotification.Should().NotBeNull();
            updatedNotification!.ReadAt.Should().Be(originalReadTime);
        }

        [Fact]
        public async Task WhenMarkingAsRead_SetsReadAtToUtcNow()
        {
            // Arrange
            using var context = DbContextHelper.CreateInMemoryContext();
            var notificationService = new NotificationService(context, _loggerMock.Object);

            var user = TestDataBuilder.CreateUser(id: "user-1");
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var notification = TestDataBuilder.CreateNotification(
                userId: user.Id,
                notificationType: NotificationType.ShareStatusChanged,
                shareId: 1,
                readAt: null
            );
            context.Notifications.Add(notification);
            await context.SaveChangesAsync();

            var beforeTime = DateTime.UtcNow.AddSeconds(-1);

            // Act
            await notificationService.MarkShareNotificationsAsReadAsync(1, user.Id);

            var afterTime = DateTime.UtcNow.AddSeconds(1);

            // Assert
            var updatedNotification = await context.Notifications.FindAsync(notification.Id);
            updatedNotification.Should().NotBeNull();
            updatedNotification!.ReadAt.Should().NotBeNull();
            updatedNotification.ReadAt.Should().BeAfter(beforeTime);
            updatedNotification.ReadAt.Should().BeBefore(afterTime);
        }

        [Fact]
        public async Task WithMultipleUnreadShareNotifications_MarksAllAsRead()
        {
            // Arrange
            using var context = DbContextHelper.CreateInMemoryContext();
            var notificationService = new NotificationService(context, _loggerMock.Object);

            var user = TestDataBuilder.CreateUser(id: "user-1");
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var notification1 = TestDataBuilder.CreateNotification(
                userId: user.Id,
                notificationType: NotificationType.ShareStatusChanged,
                shareId: 1,
                readAt: null
            );
            var notification2 = TestDataBuilder.CreateNotification(
                userId: user.Id,
                notificationType: NotificationType.ShareDueDateChanged,
                shareId: 1,
                readAt: null
            );
            var notification3 = TestDataBuilder.CreateNotification(
                userId: user.Id,
                notificationType: NotificationType.ShareStatusChanged,
                shareId: 1,
                readAt: null
            );
            context.Notifications.AddRange(notification1, notification2, notification3);
            await context.SaveChangesAsync();

            // Act
            await notificationService.MarkShareNotificationsAsReadAsync(1, user.Id);

            // Assert
            var markedAsReadCount = await context.Notifications
                .Where(n => n.UserId == user.Id && n.ShareId == 1 && n.ReadAt != null)
                .CountAsync();

            markedAsReadCount.Should().Be(3);
        }

        [Fact]
        public async Task WithNoMatchingNotifications_DoesNotThrowException()
        {
            // Arrange
            using var context = DbContextHelper.CreateInMemoryContext();
            var notificationService = new NotificationService(context, _loggerMock.Object);

            var user = TestDataBuilder.CreateUser(id: "user-1");
            context.Users.Add(user);
            await context.SaveChangesAsync();

            // Act
            var act = async () => await notificationService.MarkShareNotificationsAsReadAsync(999, user.Id);

            // Assert
            await act.Should().NotThrowAsync();
        }
    }
}
