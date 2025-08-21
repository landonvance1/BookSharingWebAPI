using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookSharingApp.Models
{
    [Table("community_user")]
    public class CommunityUser
    {
        [Key]
        [Column("community_id")]
        public int CommunityId { get; set; }
        
        [Key]
        [Column("user_id")]
        public string UserId { get; set; } = string.Empty;
        
        [ForeignKey("CommunityId")]
        public Community Community { get; set; } = null!;
        
        [ForeignKey("UserId")]
        public User User { get; set; } = null!;
    }
}