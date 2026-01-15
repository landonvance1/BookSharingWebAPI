using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookSharingApp.Migrations
{
    /// <inheritdoc />
    public partial class AddDisputeColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "disputed_by",
                table: "share",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_disputed",
                table: "share",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "disputed_by",
                table: "share");

            migrationBuilder.DropColumn(
                name: "is_disputed",
                table: "share");
        }
    }
}
