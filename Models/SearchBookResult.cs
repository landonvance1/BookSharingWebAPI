using System.ComponentModel.DataAnnotations.Schema;

namespace BookSharingApp.Models
{
    public class SearchBookResult
    {
        [Column("book_id")]
        public int BookId { get; set; }

        [Column("title")]
        public string Title { get; set; } = string.Empty;

        [Column("author")]
        public string Author { get; set; } = string.Empty;

        [Column("user_book_id")]
        public int UserBookId { get; set; }

        [Column("owner_user_id")]
        public string OwnerUserId { get; set; } = string.Empty;

        [Column("status")]
        public int Status { get; set; }

        [Column("community_id")]
        public int CommunityId { get; set; }

        [Column("community_name")]
        public string CommunityName { get; set; } = string.Empty;
    }
}