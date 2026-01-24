using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

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

        // Hide sensitive properties from JSON serialization
        [JsonIgnore]
        public override string? PasswordHash { get; set; }

        [JsonIgnore]
        public override string? SecurityStamp { get; set; }

        [JsonIgnore]
        public override string? ConcurrencyStamp { get; set; }

        [JsonIgnore]
        public override string? NormalizedUserName { get; set; }

        [JsonIgnore]
        public override string? NormalizedEmail { get; set; }

        // Hide additional system/internal properties
        [JsonIgnore]
        public override string? UserName { get; set; }

        [JsonIgnore]
        public override string? Email { get; set; }

        [JsonIgnore]
        public override bool EmailConfirmed { get; set; }

        [JsonIgnore]
        public override string? PhoneNumber { get; set; }

        [JsonIgnore]
        public override bool PhoneNumberConfirmed { get; set; }

        [JsonIgnore]
        public override bool TwoFactorEnabled { get; set; }

        [JsonIgnore]
        public override DateTimeOffset? LockoutEnd { get; set; }

        [JsonIgnore]
        public override bool LockoutEnabled { get; set; }

        [JsonIgnore]
        public override int AccessFailedCount { get; set; }

        [JsonIgnore]
        public override string Id { get; set; } = string.Empty;
    }
}