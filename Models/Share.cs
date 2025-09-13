using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BookSharingApp.Common;

namespace BookSharingApp.Models
{
    [Table("share")]
    public class Share
    {
        [Key]
        [Column("share_id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        
        [Required]
        [Column("user_book_id")]
        public int UserBookId { get; set; }
        
        [Required]
        [Column("borrower")]
        public string Borrower { get; set; } = string.Empty;
        
        [Column("return_date")]
        public DateTime? ReturnDate { get; set; }
        
        [Required]
        [Column("status")]
        public ShareStatus Status { get; set; }
        
        // Navigation properties
        public UserBook UserBook { get; set; } = null!;
        public User BorrowerUser { get; set; } = null!;
    }
}