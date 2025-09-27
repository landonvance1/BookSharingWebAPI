using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookSharingApp.Models
{
    [Table("chat_message")]
    public class ChatMessage
    {
        [Key]
        [Column("message_id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [Column("thread_id")]
        public int ThreadId { get; set; }

        [Required]
        [Column("sender_id")]
        public string SenderId { get; set; } = string.Empty;

        [Required]
        [Column("content")]
        [MaxLength(2000)]
        public string Content { get; set; } = string.Empty;

        [Required]
        [Column("sent_at")]
        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        [Column("is_system_message")]
        public bool IsSystemMessage { get; set; } = false;

        // Navigation properties
        public ChatThread Thread { get; set; } = null!;
        public User Sender { get; set; } = null!;
    }
}