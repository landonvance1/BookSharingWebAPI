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
            // Get the share to determine the other party
            var share = await _context.Shares
                .Include(s => s.UserBook)
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
                            n.NotificationType == NotificationType.ShareDueDateChanged))
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
    }
}
