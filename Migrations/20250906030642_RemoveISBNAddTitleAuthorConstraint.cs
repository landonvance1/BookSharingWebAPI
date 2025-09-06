using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookSharingApp.Migrations
{
    /// <inheritdoc />
    public partial class RemoveISBNAddTitleAuthorConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_book_isbn",
                table: "book");

            migrationBuilder.DropColumn(
                name: "isbn",
                table: "book");

            migrationBuilder.CreateIndex(
                name: "IX_book_title_author",
                table: "book",
                columns: new[] { "title", "author" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_book_title_author",
                table: "book");

            migrationBuilder.AddColumn<string>(
                name: "isbn",
                table: "book",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_book_isbn",
                table: "book",
                column: "isbn",
                unique: true);
        }
    }
}
