using BookSharingApp.Common;
using BookSharingApp.Models;
using BookSharingApp.Services;
using BookSharingApp.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace BookSharingApp.Tests.Services
{
    public class GetMessageThreadAsyncTests : IDisposable
    {
        private readonly Mock<ILogger<ChatService>> _loggerMock;
        private readonly Mock<INotificationService> _notificationServiceMock;

        public GetMessageThreadAsyncTests()
        {
            _loggerMock = new Mock<ILogger<ChatService>>();
            _notificationServiceMock = new Mock<INotificationService>();
        }

        public void Dispose()
        {
            // Cleanup if needed
        }

        [Fact]
        public async Task GetMessageThreadAsync_WithValidShare_ReturnsMessagesOrderedByDateDescending()
        {
            // Arrange
            using var context = DbContextHelper.CreateInMemoryContext();
            var chatService = new ChatService(context, _loggerMock.Object, _notificationServiceMock.Object);

            var user1 = TestDataBuilder.CreateUser(id: "user-1");
            var user2 = TestDataBuilder.CreateUser(id: "user-2");
            context.Users.AddRange(user1, user2);
            await context.SaveChangesAsync();

            var book = TestDataBuilder.CreateBook();
            context.Books.Add(book);
            await context.SaveChangesAsync();

            var userBook = new UserBook { UserId = user1.Id, BookId = book.Id, Status = UserBookStatus.Available };
            context.UserBooks.Add(userBook);
            await context.SaveChangesAsync();

            var share = new Share
            {
                UserBookId = userBook.Id,
                Borrower = user2.Id,
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

            var message1 = TestDataBuilder.CreateChatMessage(
                threadId: thread.Id,
                senderId: user1.Id,
                content: "First message",
                sentAt: DateTime.UtcNow.AddMinutes(-10)
            );
            var message2 = TestDataBuilder.CreateChatMessage(
                threadId: thread.Id,
                senderId: user2.Id,
                content: "Second message",
                sentAt: DateTime.UtcNow.AddMinutes(-5)
            );
            var message3 = TestDataBuilder.CreateChatMessage(
                threadId: thread.Id,
                senderId: user1.Id,
                content: "Third message",
                sentAt: DateTime.UtcNow
            );

            context.ChatMessages.AddRange(message1, message2, message3);
            await context.SaveChangesAsync();

            // Act
            var messages = await chatService.GetMessageThreadAsync(share.Id, page: 1, pageSize: 10);

            // Assert
            messages.Should().HaveCount(3);
            messages[0].Content.Should().Be("Third message", "messages should be ordered by SentAt descending");
            messages[1].Content.Should().Be("Second message");
            messages[2].Content.Should().Be("First message");
        }

        [Fact]
        public async Task GetMessageThreadAsync_WithPageLessThanOne_DefaultsToPageOne()
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

            var message = TestDataBuilder.CreateChatMessage(threadId: thread.Id, senderId: user.Id);
            context.ChatMessages.Add(message);
            await context.SaveChangesAsync();

            // Act
            var messages = await chatService.GetMessageThreadAsync(share.Id, page: 0, pageSize: 10);

            // Assert
            messages.Should().HaveCount(1, "page defaults to 1 when less than 1");
        }

        [Fact]
        public async Task GetMessageThreadAsync_WithInvalidPageSize_DefaultsToFifty()
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

            // Create 60 messages
            for (int i = 0; i < 60; i++)
            {
                var message = TestDataBuilder.CreateChatMessage(
                    threadId: thread.Id,
                    senderId: user.Id,
                    content: $"Message {i}",
                    sentAt: DateTime.UtcNow.AddMinutes(-i)
                );
                context.ChatMessages.Add(message);
            }
            await context.SaveChangesAsync();

            // Act - pageSize 101 should default to 50
            var messagesWithLargePageSize = await chatService.GetMessageThreadAsync(share.Id, page: 1, pageSize: 101);

            // Act - pageSize 0 should default to 50
            var messagesWithZeroPageSize = await chatService.GetMessageThreadAsync(share.Id, page: 1, pageSize: 0);

            // Assert
            messagesWithLargePageSize.Should().HaveCount(50, "pageSize > 100 should default to 50");
            messagesWithZeroPageSize.Should().HaveCount(50, "pageSize < 1 should default to 50");
        }

        [Fact]
        public async Task GetMessageThreadAsync_WhenShareChatThreadNotFound_ThrowsInvalidOperationException()
        {
            // Arrange
            using var context = DbContextHelper.CreateInMemoryContext();
            var chatService = new ChatService(context, _loggerMock.Object, _notificationServiceMock.Object);

            var nonExistentShareId = 999;

            // Act
            var act = async () => await chatService.GetMessageThreadAsync(nonExistentShareId, page: 1, pageSize: 10);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Share chat thread not found");
        }

        [Fact]
        public async Task GetMessageThreadAsync_IncludesSenderInformation()
        {
            // Arrange
            using var context = DbContextHelper.CreateInMemoryContext();
            var chatService = new ChatService(context, _loggerMock.Object, _notificationServiceMock.Object);

            var user = TestDataBuilder.CreateUser(id: "user-1", firstName: "John", lastName: "Doe");
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

            var message = TestDataBuilder.CreateChatMessage(
                threadId: thread.Id,
                senderId: user.Id,
                content: "Test message"
            );
            context.ChatMessages.Add(message);
            await context.SaveChangesAsync();

            // Act
            var messages = await chatService.GetMessageThreadAsync(share.Id, page: 1, pageSize: 10);

            // Assert
            messages.Should().HaveCount(1);
            messages[0].Sender.Should().NotBeNull();
            messages[0].Sender.FirstName.Should().Be("John");
            messages[0].Sender.LastName.Should().Be("Doe");
        }
    }
}
