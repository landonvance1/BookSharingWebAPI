using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookSharingApp.Models
{
    [Table("book")]
    public class Book
    {
        [Key]
        [Column("book_id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        
        [Required]
        [Column("title")]
        public string Title { get; set; } = string.Empty;
        
        [Required]
        [Column("author")]
        public string Author { get; set; } = string.Empty;
        
        [Required]
        [Column("isbn")]
        public string ISBN { get; set; } = string.Empty;
        
        [NotMapped]
        public string ThumbnailUrl => $"/images/{ISBN}.jpg";
    }
}