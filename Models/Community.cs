using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookSharingApp.Models
{
    [Table("community")]
    public class Community
    {
        [Key]
        [Column("community_id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        
        [Required]
        [Column("name")]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        [Column("active")]
        public bool Active { get; set; }
    }
}