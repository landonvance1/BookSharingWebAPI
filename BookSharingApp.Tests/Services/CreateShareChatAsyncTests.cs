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
    public class CreateShareChatAsyncTests : IDisposable
    {
        private readonly Mock<ILogger<ChatService>> _loggerMock;
        private readonly Mock<INotificationService> _notificationServiceMock;

        public CreateShareChatAsyncTests()
        {
            _loggerMock = new Mock<ILogger<ChatService>>();
            _notificationServiceMock = new Mock<INotificationService>();
        }

        public void Dispose()
        {
            // Cleanup if needed
        }

        [Fact]
        public async Task CreateShareChatAsync_WithNewShare_CreatesThreadAndShareChatThread()
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

            // Act
            await chatService.CreateShareChatAsync(share.Id);

            // Assert
            var shareChatThread = await context.ShareChatThreads
                .Include(sct => sct.Thread)
                .FirstOrDefaultAsync(sct => sct.ShareId == share.Id);

            shareChatThread.Should().NotBeNull();
            shareChatThread!.ShareId.Should().Be(share.Id);
            shareChatThread.Thread.Should().NotBeNull();
            shareChatThread.Thread.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
            shareChatThread.Thread.LastActivity.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task CreateShareChatAsync_WhenChatAlreadyExists_DoesNotCreateDuplicate()
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

            // Create existing chat thread
            var existingThread = TestDataBuilder.CreateChatThread();
            context.ChatThreads.Add(existingThread);
            await context.SaveChangesAsync();

            var existingShareChatThread = TestDataBuilder.CreateShareChatThread(
                threadId: existingThread.Id,
                shareId: share.Id
            );
            context.ShareChatThreads.Add(existingShareChatThread);
            await context.SaveChangesAsync();

            var initialThreadCount = await context.ChatThreads.CountAsync();

            // Act
            await chatService.CreateShareChatAsync(share.Id);

            // Assert
            var finalThreadCount = await context.ChatThreads.CountAsync();
            finalThreadCount.Should().Be(initialThreadCount, "no new thread should be created for duplicate");

            var shareChatThreadCount = await context.ShareChatThreads
                .CountAsync(sct => sct.ShareId == share.Id);
            shareChatThreadCount.Should().Be(1, "only one ShareChatThread should exist per share");
        }

        [Fact]
        public async Task CreateShareChatAsync_WithNewShare_CreatesThreadWithCorrectRelationship()
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

            // Act
            await chatService.CreateShareChatAsync(share.Id);

            // Assert
            var shareChatThread = await context.ShareChatThreads
                .Include(sct => sct.Thread)
                .Include(sct => sct.Share)
                .FirstOrDefaultAsync(sct => sct.ShareId == share.Id);

            shareChatThread.Should().NotBeNull();
            shareChatThread!.ThreadId.Should().BeGreaterThan(0);
            shareChatThread.ShareId.Should().Be(share.Id);
            shareChatThread.Thread.Should().NotBeNull();
            shareChatThread.Share.Should().NotBeNull();
            shareChatThread.Share.Id.Should().Be(share.Id);
        }
    }
}
