using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BookSharingApp.Enums;

namespace BookSharingApp.Models
{
    [Table("user_book")]
    public class UserBook
    {
        [Key]
        [Column("user_book_id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        
        [Required]
        [Column("user_id")]
        public string UserId { get; set; } = string.Empty;
        
        [Required]
        [Column("book_id")]
        public int BookId { get; set; }
        
        [Required]
        [Column("status")]
        public BookStatus Status { get; set; }
        
        // Navigation properties
        public User User { get; set; } = null!;
        public Book Book { get; set; } = null!;
    }
}