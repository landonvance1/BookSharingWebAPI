using BookSharingApp.Common;
using BookSharingApp.Models;
using BookSharingApp.Services;
using BookSharingApp.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace BookSharingApp.Tests.Services
{
    public class GetMessageCountAsyncTests : IDisposable
    {
        private readonly Mock<ILogger<ChatService>> _loggerMock;
        private readonly Mock<INotificationService> _notificationServiceMock;

        public GetMessageCountAsyncTests()
        {
            _loggerMock = new Mock<ILogger<ChatService>>();
            _notificationServiceMock = new Mock<INotificationService>();
        }

        public void Dispose()
        {
            // Cleanup if needed
        }

        [Fact]
        public async Task GetMessageCountAsync_WithExistingChatThread_ReturnsAccurateCount()
        {
            // Arrange
            using var context = DbContextHelper.CreateInMemoryContext();
            var chatService = new ChatService(context, _loggerMock.Object, _notificationServiceMock.Object);

            var user = TestDataBuilder.CreateUser();
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var book = TestDataBuilder.CreateBook();
            context.Books.Add(book);
            await context.SaveChangesAsync();

            var userBook = new UserBook { UserId = user.Id, BookId = book.Id, Status = UserBookStatus.Available };
            context.UserBooks.Add(userBook);
            await context.SaveChangesAsync();

            var share = new Share
            {
                UserBookId = userBook.Id,
                Borrower = "default-borrower",
                Status = ShareStatus.Requested
            };
            context.Shares.Add(share);
            await context.SaveChangesAsync();

            var thread = TestDataBuilder.CreateChatThread();
            context.ChatThreads.Add(thread);
            await context.SaveChangesAsync();

            var shareChatThread = TestDataBuilder.CreateShareChatThread(threadId: thread.Id, shareId: share.Id);
            context.ShareChatThreads.Add(shareChatThread);
            await context.SaveChangesAsync();

            // Create 15 messages
            for (int i = 0; i < 15; i++)
            {
                var message = TestDataBuilder.CreateChatMessage(
                    threadId: thread.Id,
                    senderId: user.Id,
                    content: $"Message {i}"
                );
                context.ChatMessages.Add(message);
            }
            await context.SaveChangesAsync();

            // Act
            var count = await chatService.GetMessageCountAsync(share.Id);

            // Assert
            count.Should().Be(15);
        }

        [Fact]
        public async Task GetMessageCountAsync_WhenChatThreadNotFound_ReturnsZero()
        {
            // Arrange
            using var context = DbContextHelper.CreateInMemoryContext();
            var chatService = new ChatService(context, _loggerMock.Object, _notificationServiceMock.Object);

            var nonExistentShareId = 999;

            // Act
            var count = await chatService.GetMessageCountAsync(nonExistentShareId);

            // Assert
            count.Should().Be(0);
        }
    }
}
