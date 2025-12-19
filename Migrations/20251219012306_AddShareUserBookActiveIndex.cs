using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookSharingApp.Migrations
{
    /// <inheritdoc />
    public partial class AddShareUserBookActiveIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE INDEX idx_share_userbook_active
                ON share(user_book_id, status)
                WHERE status IN (2, 3, 4);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS idx_share_userbook_active;");
        }
    }
}
