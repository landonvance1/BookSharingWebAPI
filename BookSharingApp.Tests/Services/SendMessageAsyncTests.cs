using BookSharingApp.Common;
using BookSharingApp.Models;
using BookSharingApp.Services;
using BookSharingApp.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace BookSharingApp.Tests.Services
{
    public class SendMessageAsyncTests : IDisposable
    {
        private readonly Mock<ILogger<ChatService>> _loggerMock;
        private readonly Mock<INotificationService> _notificationServiceMock;

        public SendMessageAsyncTests()
        {
            _loggerMock = new Mock<ILogger<ChatService>>();
            _notificationServiceMock = new Mock<INotificationService>();
        }

        public void Dispose()
        {
            // Cleanup if needed
        }

        [Fact]
        public async Task SendMessageAsync_WithValidContent_CreatesMessage()
        {
            // Arrange
            using var context = DbContextHelper.CreateInMemoryContext();
            var chatService = new ChatService(context, _loggerMock.Object, _notificationServiceMock.Object);

            var lender = TestDataBuilder.CreateUser(id: "lender-1");
            var borrower = TestDataBuilder.CreateUser(id: "borrower-1");
            var book = TestDataBuilder.CreateBook();
            var community = TestDataBuilder.CreateCommunity();

            context.Users.AddRange(lender, borrower);
            context.Books.Add(book);
            context.Communities.Add(community);
            await context.SaveChangesAsync();

            var userBook = new UserBook
            {
                UserId = lender.Id,
                BookId = book.Id,
                Status = UserBookStatus.Available
            };
            context.UserBooks.Add(userBook);

            var communityUser1 = TestDataBuilder.CreateCommunityUser(community.Id, lender.Id);
            var communityUser2 = TestDataBuilder.CreateCommunityUser(community.Id, borrower.Id);
            context.CommunityUsers.AddRange(communityUser1, communityUser2);
            await context.SaveChangesAsync();

            var share = new Share
            {
                UserBookId = userBook.Id,
                Borrower = borrower.Id,
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

            // Act
            var result = await chatService.SendMessageAsync(share.Id, borrower.Id, "Hello!");

            // Assert
            result.Should().NotBeNull();
            result.Content.Should().Be("Hello!");
            result.SenderId.Should().Be(borrower.Id);
            result.ThreadId.Should().Be(thread.Id);
            result.IsSystemMessage.Should().BeFalse();

            var messageInDb = await context.ChatMessages.FindAsync(result.Id);
            messageInDb.Should().NotBeNull();
            messageInDb!.Content.Should().Be("Hello!");
        }

        [Fact]
        public async Task SendMessageAsync_WithNullOrEmptyContent_ThrowsArgumentException()
        {
            // Arrange
            using var context = DbContextHelper.CreateInMemoryContext();
            var chatService = new ChatService(context, _loggerMock.Object, _notificationServiceMock.Object);

            var lender = TestDataBuilder.CreateUser(id: "lender-1");
            var borrower = TestDataBuilder.CreateUser(id: "borrower-1");
            var book = TestDataBuilder.CreateBook();
            var community = TestDataBuilder.CreateCommunity();

            context.Users.AddRange(lender, borrower);
            context.Books.Add(book);
            context.Communities.Add(community);
            await context.SaveChangesAsync();

            var userBook = new UserBook
            {
                UserId = lender.Id,
                BookId = book.Id,
                Status = UserBookStatus.Available
            };
            context.UserBooks.Add(userBook);

            var communityUser1 = TestDataBuilder.CreateCommunityUser(community.Id, lender.Id);
            var communityUser2 = TestDataBuilder.CreateCommunityUser(community.Id, borrower.Id);
            context.CommunityUsers.AddRange(communityUser1, communityUser2);
            await context.SaveChangesAsync();

            var share = new Share
            {
                UserBookId = userBook.Id,
                Borrower = borrower.Id,
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

            // Act & Assert - null content
            var actNull = async () => await chatService.SendMessageAsync(share.Id, borrower.Id, null!);
            await actNull.Should().ThrowAsync<ArgumentException>()
                .WithMessage("Message content is required and must be less than 2000 characters");

            // Act & Assert - empty content
            var actEmpty = async () => await chatService.SendMessageAsync(share.Id, borrower.Id, "");
            await actEmpty.Should().ThrowAsync<ArgumentException>()
                .WithMessage("Message content is required and must be less than 2000 characters");

            // Act & Assert - whitespace content
            var actWhitespace = async () => await chatService.SendMessageAsync(share.Id, borrower.Id, "   ");
            await actWhitespace.Should().ThrowAsync<ArgumentException>()
                .WithMessage("Message content is required and must be less than 2000 characters");
        }

        [Fact]
        public async Task SendMessageAsync_WithContentOver2000Characters_ThrowsArgumentException()
        {
            // Arrange
            using var context = DbContextHelper.CreateInMemoryContext();
            var chatService = new ChatService(context, _loggerMock.Object, _notificationServiceMock.Object);

            var lender = TestDataBuilder.CreateUser(id: "lender-1");
            var borrower = TestDataBuilder.CreateUser(id: "borrower-1");
            var book = TestDataBuilder.CreateBook();
            var community = TestDataBuilder.CreateCommunity();

            context.Users.AddRange(lender, borrower);
            context.Books.Add(book);
            context.Communities.Add(community);
            await context.SaveChangesAsync();

            var userBook = new UserBook
            {
                UserId = lender.Id,
                BookId = book.Id,
                Status = UserBookStatus.Available
            };
            context.UserBooks.Add(userBook);

            var communityUser1 = TestDataBuilder.CreateCommunityUser(community.Id, lender.Id);
            var communityUser2 = TestDataBuilder.CreateCommunityUser(community.Id, borrower.Id);
            context.CommunityUsers.AddRange(communityUser1, communityUser2);
            await context.SaveChangesAsync();

            var share = new Share
            {
                UserBookId = userBook.Id,
                Borrower = borrower.Id,
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

            var longContent = new string('a', 2001);

            // Act
            var act = async () => await chatService.SendMessageAsync(share.Id, borrower.Id, longContent);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("Message content is required and must be less than 2000 characters");
        }

        [Fact]
        public async Task SendMessageAsync_WhenShareNotFound_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            using var context = DbContextHelper.CreateInMemoryContext();
            var chatService = new ChatService(context, _loggerMock.Object, _notificationServiceMock.Object);

            var nonExistentShareId = 999;

            // Act
            var act = async () => await chatService.SendMessageAsync(nonExistentShareId, "user-1", "Hello!");

            // Assert
            await act.Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Access denied to this share chat");
        }

        [Fact]
        public async Task SendMessageAsync_WhenUserNotAuthorized_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            using var context = DbContextHelper.CreateInMemoryContext();
            var chatService = new ChatService(context, _loggerMock.Object, _notificationServiceMock.Object);

            var lender = TestDataBuilder.CreateUser(id: "lender-1");
            var borrower = TestDataBuilder.CreateUser(id: "borrower-1");
            var unauthorizedUser = TestDataBuilder.CreateUser(id: "unauthorized-user");
            var book = TestDataBuilder.CreateBook();
            var community = TestDataBuilder.CreateCommunity();

            context.Users.AddRange(lender, borrower, unauthorizedUser);
            context.Books.Add(book);
            context.Communities.Add(community);
            await context.SaveChangesAsync();

            var userBook = new UserBook
            {
                UserId = lender.Id,
                BookId = book.Id,
                Status = UserBookStatus.Available
            };
            context.UserBooks.Add(userBook);

            var communityUser1 = TestDataBuilder.CreateCommunityUser(community.Id, lender.Id);
            var communityUser2 = TestDataBuilder.CreateCommunityUser(community.Id, borrower.Id);
            context.CommunityUsers.AddRange(communityUser1, communityUser2);
            await context.SaveChangesAsync();

            var share = new Share
            {
                UserBookId = userBook.Id,
                Borrower = borrower.Id,
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

            // Act
            var act = async () => await chatService.SendMessageAsync(share.Id, unauthorizedUser.Id, "Hello!");

            // Assert
            await act.Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Access denied to this share chat");
        }

        [Fact]
        public async Task SendMessageAsync_TrimsContentBeforeSaving()
        {
            // Arrange
            using var context = DbContextHelper.CreateInMemoryContext();
            var chatService = new ChatService(context, _loggerMock.Object, _notificationServiceMock.Object);

            var lender = TestDataBuilder.CreateUser(id: "lender-1");
            var borrower = TestDataBuilder.CreateUser(id: "borrower-1");
            var book = TestDataBuilder.CreateBook();
            var community = TestDataBuilder.CreateCommunity();

            context.Users.AddRange(lender, borrower);
            context.Books.Add(book);
            context.Communities.Add(community);
            await context.SaveChangesAsync();

            var userBook = new UserBook
            {
                UserId = lender.Id,
                BookId = book.Id,
                Status = UserBookStatus.Available
            };
            context.UserBooks.Add(userBook);

            var communityUser1 = TestDataBuilder.CreateCommunityUser(community.Id, lender.Id);
            var communityUser2 = TestDataBuilder.CreateCommunityUser(community.Id, borrower.Id);
            context.CommunityUsers.AddRange(communityUser1, communityUser2);
            await context.SaveChangesAsync();

            var share = new Share
            {
                UserBookId = userBook.Id,
                Borrower = borrower.Id,
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

            // Act
            var result = await chatService.SendMessageAsync(share.Id, borrower.Id, "  Hello!  ");

            // Assert
            result.Content.Should().Be("Hello!", "content should be trimmed");

            var messageInDb = await context.ChatMessages.FindAsync(result.Id);
            messageInDb!.Content.Should().Be("Hello!");
        }

        [Fact]
        public async Task SendMessageAsync_UpdatesThreadLastActivity()
        {
            // Arrange
            using var context = DbContextHelper.CreateInMemoryContext();
            var chatService = new ChatService(context, _loggerMock.Object, _notificationServiceMock.Object);

            var lender = TestDataBuilder.CreateUser(id: "lender-1");
            var borrower = TestDataBuilder.CreateUser(id: "borrower-1");
            var book = TestDataBuilder.CreateBook();
            var community = TestDataBuilder.CreateCommunity();

            context.Users.AddRange(lender, borrower);
            context.Books.Add(book);
            context.Communities.Add(community);
            await context.SaveChangesAsync();

            var userBook = new UserBook
            {
                UserId = lender.Id,
                BookId = book.Id,
                Status = UserBookStatus.Available
            };
            context.UserBooks.Add(userBook);

            var communityUser1 = TestDataBuilder.CreateCommunityUser(community.Id, lender.Id);
            var communityUser2 = TestDataBuilder.CreateCommunityUser(community.Id, borrower.Id);
            context.CommunityUsers.AddRange(communityUser1, communityUser2);
            await context.SaveChangesAsync();

            var share = new Share
            {
                UserBookId = userBook.Id,
                Borrower = borrower.Id,
                Status = ShareStatus.Requested
            };
            context.Shares.Add(share);
            await context.SaveChangesAsync();

            var initialTime = DateTime.UtcNow.AddHours(-1);
            var thread = TestDataBuilder.CreateChatThread(lastActivity: initialTime);
            context.ChatThreads.Add(thread);
            await context.SaveChangesAsync();

            var shareChatThread = TestDataBuilder.CreateShareChatThread(threadId: thread.Id, shareId: share.Id);
            context.ShareChatThreads.Add(shareChatThread);
            await context.SaveChangesAsync();

            // Act
            await chatService.SendMessageAsync(share.Id, borrower.Id, "Hello!");

            // Assert
            var updatedThread = await context.ChatThreads.FindAsync(thread.Id);
            updatedThread.Should().NotBeNull();
            updatedThread!.LastActivity.Should().NotBeNull();
            updatedThread.LastActivity.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
            updatedThread.LastActivity.Should().BeAfter(initialTime);
        }

        [Fact]
        public async Task SendMessageAsync_CreatesNotificationForOtherParty()
        {
            // Arrange
            using var context = DbContextHelper.CreateInMemoryContext();
            var chatService = new ChatService(context, _loggerMock.Object, _notificationServiceMock.Object);

            var lender = TestDataBuilder.CreateUser(id: "lender-1");
            var borrower = TestDataBuilder.CreateUser(id: "borrower-1", firstName: "John", lastName: "Doe");
            var book = TestDataBuilder.CreateBook();
            var community = TestDataBuilder.CreateCommunity();

            context.Users.AddRange(lender, borrower);
            context.Books.Add(book);
            context.Communities.Add(community);
            await context.SaveChangesAsync();

            var userBook = new UserBook
            {
                UserId = lender.Id,
                BookId = book.Id,
                Status = UserBookStatus.Available
            };
            context.UserBooks.Add(userBook);

            var communityUser1 = TestDataBuilder.CreateCommunityUser(community.Id, lender.Id);
            var communityUser2 = TestDataBuilder.CreateCommunityUser(community.Id, borrower.Id);
            context.CommunityUsers.AddRange(communityUser1, communityUser2);
            await context.SaveChangesAsync();

            var share = new Share
            {
                UserBookId = userBook.Id,
                Borrower = borrower.Id,
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

            // Act
            await chatService.SendMessageAsync(share.Id, borrower.Id, "Hello!");

            // Assert
            _notificationServiceMock.Verify(
                ns => ns.CreateShareNotificationAsync(
                    share.Id,
                    NotificationType.ShareMessageReceived,
                    It.Is<string>(msg => msg.Contains("John Doe")),
                    borrower.Id
                ),
                Times.Once
            );
        }
    }
}
