using BookSharingApp.Common;
using BookSharingApp.Data;
using BookSharingApp.Models;
using Microsoft.EntityFrameworkCore;

namespace BookSharingApp.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(ApplicationDbContext context, ILogger<NotificationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task CreateShareNotificationAsync(int shareId, string notificationType, string message, string createdByUserId)
        {
            // Get the share to determine the other party, including archive states to avoid N+1 query
            var share = await _context.Shares
                .Include(s => s.UserBook)
                .Include(s => s.ShareUserStates)
                .FirstOrDefaultAsync(s => s.Id == shareId);

            if (share == null)
            {
                throw new InvalidOperationException("Share not found");
            }

            var lenderId = share.UserBook.UserId;
            var borrowerId = share.Borrower;

            // Validate that the current user is either the borrower or lender
            if (createdByUserId != lenderId && createdByUserId != borrowerId)
            {
                throw new UnauthorizedAccessException("User is not involved in this share");
            }

            // Determine the recipient (the other party)
            var recipientUserId = createdByUserId == lenderId ? borrowerId : lenderId;

            // Check if recipient has archived this share - if so, don't create notification
            if (IsShareArchivedByUser(share, recipientUserId))
            {
                _logger.LogInformation("Skipping notification for share {ShareId} - recipient {UserId} has archived the share",
                    shareId, recipientUserId);
                return;
            }

            var notification = new Notification
            {
                UserId = recipientUserId,
                NotificationType = notificationType,
                Message = message,
                ShareId = shareId,
                CreatedByUserId = createdByUserId,
                CreatedAt = DateTime.UtcNow,
                ReadAt = null
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created notification {NotificationId} for user {UserId} (type: {Type}, share: {ShareId})",
                notification.Id, recipientUserId, notificationType, shareId);
        }

        public async Task<List<Notification>> GetUnreadNotificationsAsync(string userId)
        {
            return await _context.Notifications
                .Include(n => n.Share)
                    .ThenInclude(s => s!.UserBook)
                    .ThenInclude(ub => ub.Book)
                .Include(n => n.CreatedByUser)
                .Where(n => n.UserId == userId && n.ReadAt == null)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        public async Task MarkShareNotificationsAsReadAsync(int shareId, string userId)
        {
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId &&
                           n.ShareId == shareId &&
                           n.ReadAt == null &&
                           (n.NotificationType == NotificationType.ShareStatusChanged ||
                            n.NotificationType == NotificationType.ShareDueDateChanged ||
                            n.NotificationType == NotificationType.UserBookWithdrawn))
                .ToListAsync();

            if (notifications.Any())
            {
                var now = DateTime.UtcNow;
                foreach (var notification in notifications)
                {
                    notification.ReadAt = now;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Marked {Count} share notifications as read for user {UserId} on share {ShareId}",
                    notifications.Count, userId, shareId);
            }
        }

        public async Task MarkShareChatNotificationsAsReadAsync(int shareId, string userId)
        {
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId &&
                           n.ShareId == shareId &&
                           n.ReadAt == null &&
                           n.NotificationType == NotificationType.ShareMessageReceived)
                .ToListAsync();

            if (notifications.Any())
            {
                var now = DateTime.UtcNow;
                foreach (var notification in notifications)
                {
                    notification.ReadAt = now;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Marked {Count} chat notifications as read for user {UserId} on share {ShareId}",
                    notifications.Count, userId, shareId);
            }
        }

        public async Task MarkAllShareNotificationsAsReadForUserAsync(int shareId, string userId)
        {
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId &&
                           n.ShareId == shareId &&
                           n.ReadAt == null)
                .ToListAsync();

            if (notifications.Any())
            {
                var now = DateTime.UtcNow;
                foreach (var notification in notifications)
                {
                    notification.ReadAt = now;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Marked {Count} notifications as read for user {UserId} on share {ShareId} during archive",
                    notifications.Count, userId, shareId);
            }
        }

        /// <summary>
        /// Checks if a user has archived a specific share using the pre-loaded ShareUserStates collection.
        /// </summary>
        private static bool IsShareArchivedByUser(Share share, string userId)
        {
            return share.ShareUserStates.Any(sus => sus.UserId == userId && sus.IsArchived);
        }
    }
}
