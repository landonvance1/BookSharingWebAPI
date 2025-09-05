using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookSharingApp.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueISBNConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_book_isbn",
                table: "book",
                column: "isbn",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_book_isbn",
                table: "book");
        }
    }
}
