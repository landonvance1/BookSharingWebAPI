using BookSharingApp.Data;
using Microsoft.EntityFrameworkCore;

namespace BookSharingApp.Models
{
    public class ShareChatContext : IChatContext
    {
        public int ThreadId { get; private set; }
        private readonly int _shareId;

        public ShareChatContext(int threadId, int shareId)
        {
            ThreadId = threadId;
            _shareId = shareId;
        }

        public async Task<bool> CanUserAccessAsync(string userId, ApplicationDbContext context)
        {
            var shareChatThread = await context.ShareChatThreads
                .Include(sct => sct.Share)
                    .ThenInclude(s => s.UserBook)
                .FirstOrDefaultAsync(sct => sct.ThreadId == ThreadId);

            if (shareChatThread?.Share == null) return false;

            var share = shareChatThread.Share;
            return share.Borrower == userId || share.UserBook.UserId == userId;
        }

        public async Task<IEnumerable<string>> GetAuthorizedUserIdsAsync(ApplicationDbContext context)
        {
            var shareChatThread = await context.ShareChatThreads
                .Include(sct => sct.Share)
                    .ThenInclude(s => s.UserBook)
                .FirstOrDefaultAsync(sct => sct.ThreadId == ThreadId);

            if (shareChatThread?.Share == null) return Enumerable.Empty<string>();

            var share = shareChatThread.Share;
            return new[] { share.Borrower, share.UserBook.UserId };
        }

        public async Task<ChatThread?> GetThreadAsync(ApplicationDbContext context)
        {
            return await context.ChatThreads
                .Include(ct => ct.Messages)
                    .ThenInclude(m => m.Sender)
                .FirstOrDefaultAsync(ct => ct.Id == ThreadId);
        }

        public static async Task<ShareChatContext?> CreateForShareAsync(int shareId, ApplicationDbContext context)
        {
            // Find existing thread for share
            var existingThread = await context.ShareChatThreads
                .FirstOrDefaultAsync(sct => sct.ShareId == shareId);

            if (existingThread != null)
            {
                return new ShareChatContext(existingThread.ThreadId, shareId);
            }

            // Create new thread for share
            var thread = new ChatThread();
            context.ChatThreads.Add(thread);
            await context.SaveChangesAsync();

            var shareChatThread = new ShareChatThread
            {
                ThreadId = thread.Id,
                ShareId = shareId
            };
            context.ShareChatThreads.Add(shareChatThread);
            await context.SaveChangesAsync();

            return new ShareChatContext(thread.Id, shareId);
        }
    }
}