using BookSharingApp.Common;
using BookSharingApp.Services;
using BookSharingApp.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace BookSharingApp.Tests.Services
{
    public class UpdateShareStatusAsyncTests : IDisposable
    {
        private readonly Mock<ILogger<ShareService>> _loggerMock;
        private readonly Mock<INotificationService> _notificationServiceMock;

        public UpdateShareStatusAsyncTests()
        {
            _loggerMock = new Mock<ILogger<ShareService>>();
            _notificationServiceMock = new Mock<INotificationService>();
        }

        public void Dispose()
        {
            // Cleanup if needed
        }

        [Fact]
        public async Task UpdateShareStatusAsync_WithValidStatus_UpdatesStatusAndCreatesNotification()
        {
            // Arrange
            using var context = DbContextHelper.CreateInMemoryContext();
            var shareService = new ShareService(context, _loggerMock.Object, _notificationServiceMock.Object);

            var lender = TestDataBuilder.CreateUser(id: "lender-1", firstName: "John", lastName: "Doe");
            var borrower = TestDataBuilder.CreateUser(id: "borrower-1");
            var book = TestDataBuilder.CreateBook(title: "The Great Gatsby");

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
                status: ShareStatus.Requested,
                userBook: userBook,
                borrowerUser: borrower
            );
            context.Shares.Add(share);
            await context.SaveChangesAsync();

            // Act
            await shareService.UpdateShareStatusAsync(share.Id, ShareStatus.Ready, lender.Id);

            // Assert
            var updatedShare = await context.Shares.FindAsync(share.Id);
            updatedShare.Should().NotBeNull();
            updatedShare!.Status.Should().Be(ShareStatus.Ready);

            _notificationServiceMock.Verify(
                x => x.CreateShareNotificationAsync(
                    share.Id,
                    NotificationType.ShareStatusChanged,
                    It.Is<string>(msg => msg.Contains("ready for pickup")),
                    lender.Id
                ),
                Times.Once
            );
        }

        [Fact]
        public async Task UpdateShareStatusAsync_ToPickedUp_UpdatesStatusWithCorrectMessage()
        {
            // Arrange
            using var context = DbContextHelper.CreateInMemoryContext();
            var shareService = new ShareService(context, _loggerMock.Object, _notificationServiceMock.Object);

            var lender = TestDataBuilder.CreateUser(id: "lender-1");
            var borrower = TestDataBuilder.CreateUser(id: "borrower-1", firstName: "Jane", lastName: "Smith");
            var book = TestDataBuilder.CreateBook(title: "Test Book");

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
            await shareService.UpdateShareStatusAsync(share.Id, ShareStatus.PickedUp, borrower.Id);

            // Assert
            var updatedShare = await context.Shares.FindAsync(share.Id);
            updatedShare!.Status.Should().Be(ShareStatus.PickedUp);

            _notificationServiceMock.Verify(
                x => x.CreateShareNotificationAsync(
                    share.Id,
                    NotificationType.ShareStatusChanged,
                    It.Is<string>(msg => msg.Contains("picked up") && msg.Contains("Jane Smith")),
                    borrower.Id
                ),
                Times.Once
            );
        }

        [Fact]
        public async Task UpdateShareStatusAsync_WhenShareNotFound_ThrowsInvalidOperationException()
        {
            // Arrange
            using var context = DbContextHelper.CreateInMemoryContext();
            var shareService = new ShareService(context, _loggerMock.Object, _notificationServiceMock.Object);

            var user = TestDataBuilder.CreateUser(id: "user-1");
            context.Users.Add(user);
            await context.SaveChangesAsync();

            // Act
            var act = async () => await shareService.UpdateShareStatusAsync(999, ShareStatus.Ready, user.Id);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Share not found");
        }

        [Fact]
        public async Task UpdateShareStatusAsync_ToHomeSafe_UpdatesStatusAndPersistsToDatabase()
        {
            // Arrange
            using var context = DbContextHelper.CreateInMemoryContext();
            var shareService = new ShareService(context, _loggerMock.Object, _notificationServiceMock.Object);

            var lender = TestDataBuilder.CreateUser(id: "lender-1");
            var borrower = TestDataBuilder.CreateUser(id: "borrower-1");
            var book = TestDataBuilder.CreateBook(title: "Book Title");

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
            await shareService.UpdateShareStatusAsync(share.Id, ShareStatus.HomeSafe, lender.Id);

            // Assert
            var updatedShare = await context.Shares.FindAsync(share.Id);
            updatedShare!.Status.Should().Be(ShareStatus.HomeSafe);

            _notificationServiceMock.Verify(
                x => x.CreateShareNotificationAsync(
                    share.Id,
                    NotificationType.ShareStatusChanged,
                    It.Is<string>(msg => msg.Contains("confirmed home safe")),
                    lender.Id
                ),
                Times.Once
            );
        }
    }
}
