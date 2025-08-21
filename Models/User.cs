using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookSharingApp.Models
{
    [Table("aspnetusers")]
    public class User : IdentityUser
    {
        [Required]
        [Column("first_name")]
        public string FirstName { get; set; } = string.Empty;
        
        [Required]
        [Column("last_name")]
        public string LastName { get; set; } = string.Empty;
        
        [NotMapped]
        public string FullName => $"{FirstName} {LastName}";
        
        public ICollection<CommunityUser> JoinedCommunities { get; set; } = new List<CommunityUser>();
    }
}