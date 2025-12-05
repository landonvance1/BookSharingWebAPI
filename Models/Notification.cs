using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookSharingApp.Models
{
    [Table("notification")]
    public class Notification
    {
        [Key]
        [Column("notification_id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [Column("user_id")]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [Column("notification_type")]
        [MaxLength(50)]
        public string NotificationType { get; set; } = string.Empty;

        [Required]
        [Column("message")]
        [MaxLength(500)]
        public string Message { get; set; } = string.Empty;

        [Required]
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("read_at")]
        public DateTime? ReadAt { get; set; }

        [Column("share_id")]
        public int? ShareId { get; set; }

        [Required]
        [Column("created_by_user_id")]
        public string CreatedByUserId { get; set; } = string.Empty;

        // Navigation properties
        public User User { get; set; } = null!;
        public Share? Share { get; set; }
        public User CreatedByUser { get; set; } = null!;
    }
}
