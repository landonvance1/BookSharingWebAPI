using BookSharingApp.Common;
using BookSharingApp.Data;
using BookSharingApp.Models;
using BookSharingApp.Validators;
using Microsoft.EntityFrameworkCore;

namespace BookSharingApp.Services
{
    public class ShareService : IShareService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ShareService> _logger;
        private readonly INotificationService _notificationService;

        public ShareService(ApplicationDbContext context, ILogger<ShareService> logger, INotificationService notificationService)
        {
            _context = context;
            _logger = logger;
            _notificationService = notificationService;
        }

        // Reads - simple pass-through to EF
        public async Task<Share?> GetShareAsync(int id)
        {
            return await _context.Shares
                .Include(s => s.UserBook)
                    .ThenInclude(ub => ub.Book)
                .Include(s => s.UserBook)
                    .ThenInclude(ub => ub.User)
                .Include(s => s.BorrowerUser)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<List<Share>> GetBorrowerSharesAsync(string userId)
        {
            return await _context.Shares
                .Include(s => s.UserBook)
                    .ThenInclude(ub => ub.Book)
                .Include(s => s.UserBook)
                    .ThenInclude(ub => ub.User)
                .Where(s => s.Borrower == userId &&
                    !_context.ShareUserStates.Any(sus => sus.ShareId == s.Id && sus.UserId == userId && sus.IsArchived))
                .ToListAsync();
        }

        public async Task<List<Share>> GetLenderSharesAsync(string userId)
        {
            return await _context.Shares
                .Include(s => s.UserBook)
                    .ThenInclude(ub => ub.Book)
                .Include(s => s.BorrowerUser)
                .Where(s => s.UserBook.UserId == userId &&
                    !_context.ShareUserStates.Any(sus => sus.ShareId == s.Id && sus.UserId == userId && sus.IsArchived))
                .ToListAsync();
        }

        public async Task<List<Share>> GetArchivedBorrowerSharesAsync(string userId)
        {
            return await _context.Shares
                .Include(s => s.UserBook)
                    .ThenInclude(ub => ub.Book)
                .Include(s => s.UserBook)
                    .ThenInclude(ub => ub.User)
                .Where(s => s.Borrower == userId &&
                    _context.ShareUserStates.Any(sus => sus.ShareId == s.Id && sus.UserId == userId && sus.IsArchived))
                .ToListAsync();
        }

        public async Task<List<Share>> GetArchivedLenderSharesAsync(string userId)
        {
            return await _context.Shares
                .Include(s => s.UserBook)
                    .ThenInclude(ub => ub.Book)
                .Include(s => s.BorrowerUser)
                .Where(s => s.UserBook.UserId == userId &&
                    _context.ShareUserStates.Any(sus => sus.ShareId == s.Id && sus.UserId == userId && sus.IsArchived))
                .ToListAsync();
        }

        // Writes - intent-based methods with business logic
        public async Task<Share> CreateShareAsync(int userBookId, string borrowerId)
        {
            // Check if userbook exists
            var userBook = await _context.UserBooks
                .Include(ub => ub.User)
                .FirstOrDefaultAsync(ub => ub.Id == userBookId);

            if (userBook is null)
                throw new InvalidOperationException("UserBook not found");

            // Check if user is trying to borrow their own book
            if (userBook.UserId == borrowerId)
                throw new InvalidOperationException("Cannot borrow your own book");

            // Check if userbook is available
            if (userBook.Status != UserBookStatus.Available)
                throw new InvalidOperationException("Book is not available for sharing");

            // Check if borrower and lender share at least one community
            var borrowerCommunities = await _context.CommunityUsers
                .Where(cu => cu.UserId == borrowerId)
                .Select(cu => cu.CommunityId)
                .ToListAsync();

            var lenderCommunities = await _context.CommunityUsers
                .Where(cu => cu.UserId == userBook.UserId)
                .Select(cu => cu.CommunityId)
                .ToListAsync();

            var sharedCommunities = borrowerCommunities.Intersect(lenderCommunities).Any();
            if (!sharedCommunities)
                throw new InvalidOperationException("You must share a community with the book owner to request this book");

            // Check if there's already an active share for this userbook by this borrower
            var existingShare = await _context.Shares
                .FirstOrDefaultAsync(s => s.UserBookId == userBookId &&
                                        s.Borrower == borrowerId &&
                                        s.Status <= ShareStatus.Returned);
            if (existingShare != null)
                throw new InvalidOperationException("You already have an active share request for this book");

            var share = new Share
            {
                UserBookId = userBookId,
                Borrower = borrowerId,
                ReturnDate = null,
                Status = ShareStatus.Requested
            };

            _context.Shares.Add(share);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created share {ShareId} for userbook {UserBookId} by borrower {BorrowerId}",
                share.Id, userBookId, borrowerId);

            // Create notification for lender about new share request
            await _notificationService.CreateShareNotificationAsync(
                share.Id,
                Common.NotificationType.ShareStatusChanged,
                $"New share request for '{userBook.Book?.Title ?? "a book"}' from {await GetUserNameAsync(borrowerId)}",
                borrowerId);

            return share;
        }

        public async Task UpdateShareStatusAsync(int shareId, ShareStatus newStatus, string updatedByUserId)
        {
            // Find the share with required includes
            var share = await _context.Shares
                .Include(s => s.UserBook)
                    .ThenInclude(ub => ub.Book)
                .Include(s => s.UserBook)
                    .ThenInclude(ub => ub.User)
                .Include(s => s.BorrowerUser)
                .FirstOrDefaultAsync(s => s.Id == shareId);

            if (share is null)
                throw new InvalidOperationException("Share not found");

            // Validate the status transition
            var validator = new ShareStatusValidator();
            var validationResult = validator.ValidateStatusTransition(share, newStatus, updatedByUserId);

            if (!validationResult.IsValid)
                throw new InvalidOperationException(validationResult.ErrorMessage);

            // Update the status
            share.Status = newStatus;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated share {ShareId} status to {Status} by user {UserId}",
                shareId, newStatus, updatedByUserId);

            // Create notification for the other party about status change
            var statusText = newStatus switch
            {
                ShareStatus.Requested => "requested",
                ShareStatus.Ready => "ready for pickup",
                ShareStatus.PickedUp => "picked up",
                ShareStatus.Returned => "returned",
                ShareStatus.HomeSafe => "confirmed home safe",
                ShareStatus.Disputed => "disputed",
                ShareStatus.Declined => "declined",
                _ => newStatus.ToString()
            };

            var updaterName = await GetUserNameAsync(updatedByUserId);
            var bookTitle = share.UserBook?.Book?.Title ?? "the book";

            await _notificationService.CreateShareNotificationAsync(
                shareId,
                Common.NotificationType.ShareStatusChanged,
                $"'{bookTitle}' share is now {statusText} by {updaterName}",
                updatedByUserId);
        }

        public async Task UpdateShareDueDateAsync(int shareId, DateTime newDueDate, string updatedByUserId)
        {
            // Find the share with required includes
            var share = await _context.Shares
                .Include(s => s.UserBook)
                    .ThenInclude(ub => ub.Book)
                .Include(s => s.UserBook)
                    .ThenInclude(ub => ub.User)
                .Include(s => s.BorrowerUser)
                .FirstOrDefaultAsync(s => s.Id == shareId);

            if (share is null)
                throw new InvalidOperationException("Share not found");

            // Verify that current user is the lender (owner of the userbook)
            if (share.UserBook.UserId != updatedByUserId)
                throw new UnauthorizedAccessException("Only the lender can set the return date");

            // Set the return date
            share.ReturnDate = newDueDate;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated share {ShareId} return date to {ReturnDate} by user {UserId}",
                shareId, newDueDate, updatedByUserId);

            // Create notification for borrower about due date change
            var lenderName = await GetUserNameAsync(updatedByUserId);
            var bookTitle = share.UserBook?.Book?.Title ?? "the book";
            var dueDateFormatted = newDueDate.ToString("MMMM dd, yyyy");

            await _notificationService.CreateShareNotificationAsync(
                shareId,
                Common.NotificationType.ShareDueDateChanged,
                $"Return date for '{bookTitle}' updated to {dueDateFormatted} by {lenderName}",
                updatedByUserId);
        }

        public async Task ArchiveShareAsync(int shareId, string archivedByUserId)
        {
            // Find the share with required includes
            var share = await _context.Shares
                .Include(s => s.UserBook)
                .FirstOrDefaultAsync(s => s.Id == shareId);

            if (share is null)
                throw new InvalidOperationException("Share not found");

            // Verify that current user is the lender or borrower
            if (share.UserBook.UserId != archivedByUserId && share.Borrower != archivedByUserId)
                throw new UnauthorizedAccessException("Only the lender or borrower can archive the share");

            // Verify that share is in terminal status
            if (share.Status != ShareStatus.Declined &&
                share.Status != ShareStatus.Disputed &&
                share.Status != ShareStatus.HomeSafe)
            {
                throw new InvalidOperationException("Can only archive shares in terminal status (Declined, Disputed, or HomeSafe)");
            }

            // Check if already archived
            var existingState = await _context.ShareUserStates
                .FirstOrDefaultAsync(sus => sus.ShareId == shareId && sus.UserId == archivedByUserId);

            if (existingState != null)
            {
                if (existingState.IsArchived)
                    throw new InvalidOperationException("Share is already archived");

                // Update existing state
                existingState.IsArchived = true;
                existingState.ArchivedAt = DateTime.UtcNow;
            }
            else
            {
                // Create new state
                var shareUserState = new ShareUserState
                {
                    ShareId = shareId,
                    UserId = archivedByUserId,
                    IsArchived = true,
                    ArchivedAt = DateTime.UtcNow
                };
                _context.ShareUserStates.Add(shareUserState);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Archived share {ShareId} for user {UserId}", shareId, archivedByUserId);
        }

        public async Task UnarchiveShareAsync(int shareId, string unarchivedByUserId)
        {
            // Find the share
            var share = await _context.Shares
                .Include(s => s.UserBook)
                .FirstOrDefaultAsync(s => s.Id == shareId);

            if (share is null)
                throw new InvalidOperationException("Share not found");

            // Verify that current user is the lender or borrower
            if (share.UserBook.UserId != unarchivedByUserId && share.Borrower != unarchivedByUserId)
                throw new UnauthorizedAccessException("Only the lender or borrower can unarchive the share");

            // Find the share user state
            var shareUserState = await _context.ShareUserStates
                .FirstOrDefaultAsync(sus => sus.ShareId == shareId && sus.UserId == unarchivedByUserId);

            if (shareUserState is null || !shareUserState.IsArchived)
                throw new InvalidOperationException("Share is not archived");

            // Update state to unarchived
            shareUserState.IsArchived = false;
            shareUserState.ArchivedAt = null;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Unarchived share {ShareId} for user {UserId}", shareId, unarchivedByUserId);
        }

        // Helper methods
        private async Task<string> GetUserNameAsync(string userId)
        {
            var user = await _context.Users.FindAsync(userId);
            return user != null ? user.FullName : "Someone";
        }
    }
}
