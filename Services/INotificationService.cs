using BookSharingApp.Models;

namespace BookSharingApp.Services
{
    public interface INotificationService
    {
        // Create notification
        Task CreateShareNotificationAsync(int shareId, string notificationType, string message, string createdByUserId);

        // Get notifications
        Task<List<Notification>> GetUnreadNotificationsAsync(string userId);

        // Mark notifications as read
        Task MarkShareNotificationsAsReadAsync(int shareId, string userId);
        Task MarkShareChatNotificationsAsReadAsync(int shareId, string userId);
    }
}
