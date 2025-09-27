using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookSharingApp.Models
{
    [Table("share_chat_thread")]
    public class ShareChatThread
    {
        [Key]
        [Column("thread_id")]
        public int ThreadId { get; set; }

        [Required]
        [Column("share_id")]
        public int ShareId { get; set; }

        // Navigation properties
        public ChatThread Thread { get; set; } = null!;
        public Share Share { get; set; } = null!;
    }
}