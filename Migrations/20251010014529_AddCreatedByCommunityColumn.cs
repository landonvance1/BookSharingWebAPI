using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookSharingApp.Migrations
{
    /// <inheritdoc />
    public partial class AddCreatedByCommunityColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "created_by",
                table: "community",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_community_created_by",
                table: "community",
                column: "created_by");

            migrationBuilder.AddForeignKey(
                name: "FK_community_AspNetUsers_created_by",
                table: "community",
                column: "created_by",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_community_AspNetUsers_created_by",
                table: "community");

            migrationBuilder.DropIndex(
                name: "IX_community_created_by",
                table: "community");

            migrationBuilder.DropColumn(
                name: "created_by",
                table: "community");
        }
    }
}
