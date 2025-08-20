using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookSharingApp.Models
{
    [Table("refresh_tokens")]
    public class RefreshToken
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        
        [Required]
        [Column("token")]
        public string Token { get; set; } = string.Empty;
        
        [Required]
        [Column("user_id")]
        public string UserId { get; set; } = string.Empty;
        
        [Required]
        [Column("expires_at")]
        public DateTime ExpiresAt { get; set; }
        
        [Required]
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        [Column("revoked_at")]
        public DateTime? RevokedAt { get; set; }
        
        [NotMapped]
        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
        
        [NotMapped]
        public bool IsRevoked => RevokedAt.HasValue;
        
        [NotMapped]
        public bool IsActive => !IsExpired && !IsRevoked;
        
        [ForeignKey(nameof(UserId))]
        public virtual User User { get; set; } = null!;
    }
}