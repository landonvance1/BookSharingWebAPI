using BookSharingApp.Common;
using BookSharingApp.Services;
using BookSharingApp.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace BookSharingApp.Tests.Services
{
    public class CreateShareAsyncTests : IDisposable
    {
        private readonly Mock<ILogger<ShareService>> _loggerMock;
        private readonly Mock<INotificationService> _notificationServiceMock;

        public CreateShareAsyncTests()
        {
            _loggerMock = new Mock<ILogger<ShareService>>();
            _notificationServiceMock = new Mock<INotificationService>();
        }

        public void Dispose()
        {
            // Cleanup if needed
        }

        [Fact]
        public async Task CreateShareAsync_WithValidRequest_CreatesShareWithRequestedStatus()
        {
            // Arrange
            using var context = DbContextHelper.CreateInMemoryContext();
            var shareService = new ShareService(context, _loggerMock.Object, _notificationServiceMock.Object);

            var lender = TestDataBuilder.CreateUser(id: "lender-1", firstName: "John", lastName: "Doe");
            var borrower = TestDataBuilder.CreateUser(id: "borrower-1", firstName: "Jane", lastName: "Smith");
            var book = TestDataBuilder.CreateBook(title: "The Great Gatsby", author: "F. Scott Fitzgerald");
            var community = TestDataBuilder.CreateCommunity(name: "Book Club");

            context.Users.AddRange(lender, borrower);
            context.Books.Add(book);
            context.Communities.Add(community);
            await context.SaveChangesAsync();

            var userBook = TestDataBuilder.CreateUserBook(
                userId: lender.Id,
                bookId: book.Id,
                status: UserBookStatus.Available,
                user: lender,
                book: book
            );
            context.UserBooks.Add(userBook);

            var communityUser1 = TestDataBuilder.CreateCommunityUser(community.Id, lender.Id);
            var communityUser2 = TestDataBuilder.CreateCommunityUser(community.Id, borrower.Id);
            context.CommunityUsers.AddRange(communityUser1, communityUser2);
            await context.SaveChangesAsync();

            // Act
            var result = await shareService.CreateShareAsync(userBook.Id, borrower.Id);

            // Assert
            result.Should().NotBeNull();
            result.UserBookId.Should().Be(userBook.Id);
            result.Borrower.Should().Be(borrower.Id);
            result.Status.Should().Be(ShareStatus.Requested);
            result.ReturnDate.Should().BeNull();

            var shareInDb = await context.Shares.FindAsync(result.Id);
            shareInDb.Should().NotBeNull();
            shareInDb!.Status.Should().Be(ShareStatus.Requested);
        }

        [Fact]
        public async Task CreateShareAsync_WithValidRequest_CreatesNotificationForLender()
        {
            // Arrange
            using var context = DbContextHelper.CreateInMemoryContext();
            var shareService = new ShareService(context, _loggerMock.Object, _notificationServiceMock.Object);

            var lender = TestDataBuilder.CreateUser(id: "lender-1");
            var borrower = TestDataBuilder.CreateUser(id: "borrower-1");
            var book = TestDataBuilder.CreateBook(title: "Test Book");
            var community = TestDataBuilder.CreateCommunity();

            context.Users.AddRange(lender, borrower);
            context.Books.Add(book);
            context.Communities.Add(community);
            await context.SaveChangesAsync();

            var userBook = TestDataBuilder.CreateUserBook(
                userId: lender.Id,
                bookId: book.Id,
                status: UserBookStatus.Available,
                user: lender,
                book: book
            );
            context.UserBooks.Add(userBook);

            context.CommunityUsers.AddRange(
                TestDataBuilder.CreateCommunityUser(community.Id, lender.Id),
                TestDataBuilder.CreateCommunityUser(community.Id, borrower.Id)
            );
            await context.SaveChangesAsync();

            // Act
            await shareService.CreateShareAsync(userBook.Id, borrower.Id);

            // Assert
            _notificationServiceMock.Verify(
                x => x.CreateShareNotificationAsync(
                    It.IsAny<int>(),
                    NotificationType.ShareStatusChanged,
                    It.Is<string>(msg => msg.Contains("New share request")),
                    borrower.Id
                ),
                Times.Once
            );
        }

        [Fact]
        public async Task CreateShareAsync_WhenUserBookNotFound_ThrowsInvalidOperationException()
        {
            // Arrange
            using var context = DbContextHelper.CreateInMemoryContext();
            var shareService = new ShareService(context, _loggerMock.Object, _notificationServiceMock.Object);

            var borrower = TestDataBuilder.CreateUser(id: "borrower-1");
            context.Users.Add(borrower);
            await context.SaveChangesAsync();

            // Act
            var act = async () => await shareService.CreateShareAsync(999, borrower.Id);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("UserBook not found");
        }

        [Fact]
        public async Task CreateShareAsync_WhenBorrowingOwnBook_ThrowsInvalidOperationException()
        {
            // Arrange
            using var context = DbContextHelper.CreateInMemoryContext();
            var shareService = new ShareService(context, _loggerMock.Object, _notificationServiceMock.Object);

            var user = TestDataBuilder.CreateUser(id: "user-1");
            var book = TestDataBuilder.CreateBook();

            context.Users.Add(user);
            context.Books.Add(book);
            await context.SaveChangesAsync();

            var userBook = TestDataBuilder.CreateUserBook(
                userId: user.Id,
                bookId: book.Id,
                status: UserBookStatus.Available,
                user: user,
                book: book
            );
            context.UserBooks.Add(userBook);
            await context.SaveChangesAsync();

            // Act
            var act = async () => await shareService.CreateShareAsync(userBook.Id, user.Id);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Cannot borrow your own book");
        }

        [Fact]
        public async Task CreateShareAsync_WhenBookNotAvailable_ThrowsInvalidOperationException()
        {
            // Arrange
            using var context = DbContextHelper.CreateInMemoryContext();
            var shareService = new ShareService(context, _loggerMock.Object, _notificationServiceMock.Object);

            var lender = TestDataBuilder.CreateUser(id: "lender-1");
            var borrower = TestDataBuilder.CreateUser(id: "borrower-1");
            var book = TestDataBuilder.CreateBook();
            var community = TestDataBuilder.CreateCommunity();

            context.Users.AddRange(lender, borrower);
            context.Books.Add(book);
            context.Communities.Add(community);
            await context.SaveChangesAsync();

            var userBook = TestDataBuilder.CreateUserBook(
                userId: lender.Id,
                bookId: book.Id,
                status: UserBookStatus.Unavailable,  // Book is NOT available
                user: lender,
                book: book
            );
            context.UserBooks.Add(userBook);

            context.CommunityUsers.AddRange(
                TestDataBuilder.CreateCommunityUser(community.Id, lender.Id),
                TestDataBuilder.CreateCommunityUser(community.Id, borrower.Id)
            );
            await context.SaveChangesAsync();

            // Act
            var act = async () => await shareService.CreateShareAsync(userBook.Id, borrower.Id);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Book is not available for sharing");
        }

        [Fact]
        public async Task CreateShareAsync_WhenNoSharedCommunity_ThrowsInvalidOperationException()
        {
            // Arrange
            using var context = DbContextHelper.CreateInMemoryContext();
            var shareService = new ShareService(context, _loggerMock.Object, _notificationServiceMock.Object);

            var lender = TestDataBuilder.CreateUser(id: "lender-1");
            var borrower = TestDataBuilder.CreateUser(id: "borrower-1");
            var book = TestDataBuilder.CreateBook();
            var lenderCommunity = TestDataBuilder.CreateCommunity(name: "Lender Community");
            var borrowerCommunity = TestDataBuilder.CreateCommunity(name: "Borrower Community");

            context.Users.AddRange(lender, borrower);
            context.Books.Add(book);
            context.Communities.AddRange(lenderCommunity, borrowerCommunity);
            await context.SaveChangesAsync();

            var userBook = TestDataBuilder.CreateUserBook(
                userId: lender.Id,
                bookId: book.Id,
                status: UserBookStatus.Available,
                user: lender,
                book: book
            );
            context.UserBooks.Add(userBook);

            // Users are in different communities - no shared community
            context.CommunityUsers.AddRange(
                TestDataBuilder.CreateCommunityUser(lenderCommunity.Id, lender.Id),
                TestDataBuilder.CreateCommunityUser(borrowerCommunity.Id, borrower.Id)
            );
            await context.SaveChangesAsync();

            // Act
            var act = async () => await shareService.CreateShareAsync(userBook.Id, borrower.Id);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("You must share a community with the book owner to request this book");
        }

        [Fact]
        public async Task CreateShareAsync_WhenActiveShareAlreadyExists_ThrowsInvalidOperationException()
        {
            // Arrange
            using var context = DbContextHelper.CreateInMemoryContext();
            var shareService = new ShareService(context, _loggerMock.Object, _notificationServiceMock.Object);

            var lender = TestDataBuilder.CreateUser(id: "lender-1");
            var borrower = TestDataBuilder.CreateUser(id: "borrower-1");
            var book = TestDataBuilder.CreateBook();
            var community = TestDataBuilder.CreateCommunity();

            context.Users.AddRange(lender, borrower);
            context.Books.Add(book);
            context.Communities.Add(community);
            await context.SaveChangesAsync();

            var userBook = TestDataBuilder.CreateUserBook(
                userId: lender.Id,
                bookId: book.Id,
                status: UserBookStatus.Available,
                user: lender,
                book: book
            );
            context.UserBooks.Add(userBook);

            context.CommunityUsers.AddRange(
                TestDataBuilder.CreateCommunityUser(community.Id, lender.Id),
                TestDataBuilder.CreateCommunityUser(community.Id, borrower.Id)
            );
            await context.SaveChangesAsync();

            // Create an existing active share (status <= Returned)
            var existingShare = TestDataBuilder.CreateShare(
                userBookId: userBook.Id,
                borrower: borrower.Id,
                status: ShareStatus.Requested,
                userBook: userBook,
                borrowerUser: borrower
            );
            context.Shares.Add(existingShare);
            await context.SaveChangesAsync();

            // Act
            var act = async () => await shareService.CreateShareAsync(userBook.Id, borrower.Id);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("You already have an active share request for this book");
        }

        [Fact]
        public async Task CreateShareAsync_WhenPreviousShareIsTerminal_AllowsNewShareRequest()
        {
            // Arrange
            using var context = DbContextHelper.CreateInMemoryContext();
            var shareService = new ShareService(context, _loggerMock.Object, _notificationServiceMock.Object);

            var lender = TestDataBuilder.CreateUser(id: "lender-1");
            var borrower = TestDataBuilder.CreateUser(id: "borrower-1");
            var book = TestDataBuilder.CreateBook();
            var community = TestDataBuilder.CreateCommunity();

            context.Users.AddRange(lender, borrower);
            context.Books.Add(book);
            context.Communities.Add(community);
            await context.SaveChangesAsync();

            var userBook = TestDataBuilder.CreateUserBook(
                userId: lender.Id,
                bookId: book.Id,
                status: UserBookStatus.Available,
                user: lender,
                book: book
            );
            context.UserBooks.Add(userBook);

            context.CommunityUsers.AddRange(
                TestDataBuilder.CreateCommunityUser(community.Id, lender.Id),
                TestDataBuilder.CreateCommunityUser(community.Id, borrower.Id)
            );
            await context.SaveChangesAsync();

            // Create a previous share in terminal state (HomeSafe)
            var previousShare = TestDataBuilder.CreateShare(
                userBookId: userBook.Id,
                borrower: borrower.Id,
                status: ShareStatus.HomeSafe,  // Terminal state
                userBook: userBook,
                borrowerUser: borrower
            );
            context.Shares.Add(previousShare);
            await context.SaveChangesAsync();

            // Act
            var result = await shareService.CreateShareAsync(userBook.Id, borrower.Id);

            // Assert
            result.Should().NotBeNull();
            result.Status.Should().Be(ShareStatus.Requested);
        }

        [Fact]
        public async Task CreateShareAsync_WithMultipleSharedCommunities_CreatesShare()
        {
            // Arrange
            using var context = DbContextHelper.CreateInMemoryContext();
            var shareService = new ShareService(context, _loggerMock.Object, _notificationServiceMock.Object);

            var lender = TestDataBuilder.CreateUser(id: "lender-1");
            var borrower = TestDataBuilder.CreateUser(id: "borrower-1");
            var book = TestDataBuilder.CreateBook();
            var community1 = TestDataBuilder.CreateCommunity(name: "Community 1");
            var community2 = TestDataBuilder.CreateCommunity(name: "Community 2");

            context.Users.AddRange(lender, borrower);
            context.Books.Add(book);
            context.Communities.AddRange(community1, community2);
            await context.SaveChangesAsync();

            var userBook = TestDataBuilder.CreateUserBook(
                userId: lender.Id,
                bookId: book.Id,
                status: UserBookStatus.Available,
                user: lender,
                book: book
            );
            context.UserBooks.Add(userBook);

            // Both users share multiple communities
            context.CommunityUsers.AddRange(
                TestDataBuilder.CreateCommunityUser(community1.Id, lender.Id),
                TestDataBuilder.CreateCommunityUser(community1.Id, borrower.Id),
                TestDataBuilder.CreateCommunityUser(community2.Id, lender.Id),
                TestDataBuilder.CreateCommunityUser(community2.Id, borrower.Id)
            );
            await context.SaveChangesAsync();

            // Act
            var result = await shareService.CreateShareAsync(userBook.Id, borrower.Id);

            // Assert
            result.Should().NotBeNull();
            result.Status.Should().Be(ShareStatus.Requested);
        }

        [Fact]
        public async Task CreateShareAsync_WhenBorrowerInNoCommunities_ThrowsInvalidOperationException()
        {
            // Arrange
            using var context = DbContextHelper.CreateInMemoryContext();
            var shareService = new ShareService(context, _loggerMock.Object, _notificationServiceMock.Object);

            var lender = TestDataBuilder.CreateUser(id: "lender-1");
            var borrower = TestDataBuilder.CreateUser(id: "borrower-1");
            var book = TestDataBuilder.CreateBook();
            var community = TestDataBuilder.CreateCommunity();

            context.Users.AddRange(lender, borrower);
            context.Books.Add(book);
            context.Communities.Add(community);
            await context.SaveChangesAsync();

            var userBook = TestDataBuilder.CreateUserBook(
                userId: lender.Id,
                bookId: book.Id,
                status: UserBookStatus.Available,
                user: lender,
                book: book
            );
            context.UserBooks.Add(userBook);

            // Only lender is in a community, borrower is not
            context.CommunityUsers.Add(TestDataBuilder.CreateCommunityUser(community.Id, lender.Id));
            await context.SaveChangesAsync();

            // Act
            var act = async () => await shareService.CreateShareAsync(userBook.Id, borrower.Id);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("You must share a community with the book owner to request this book");
        }
    }
}
