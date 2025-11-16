using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookSharingApp.Models
{
    [Table("share_user_state")]
    public class ShareUserState
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [Column("share_id")]
        public int ShareId { get; set; }

        [Required]
        [Column("user_id")]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [Column("is_archived")]
        public bool IsArchived { get; set; }

        [Column("archived_at")]
        public DateTime? ArchivedAt { get; set; }

        // Navigation properties
        public Share Share { get; set; } = null!;
        public User User { get; set; } = null!;
    }
}
