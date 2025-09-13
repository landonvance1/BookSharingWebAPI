using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BookSharingApp.Migrations
{
    /// <inheritdoc />
    public partial class AddShareTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "share",
                columns: table => new
                {
                    share_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_book_id = table.Column<int>(type: "integer", nullable: false),
                    borrower = table.Column<string>(type: "text", nullable: false),
                    return_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_share", x => x.share_id);
                    table.ForeignKey(
                        name: "FK_share_AspNetUsers_borrower",
                        column: x => x.borrower,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_share_user_book_user_book_id",
                        column: x => x.user_book_id,
                        principalTable: "user_book",
                        principalColumn: "user_book_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_share_borrower",
                table: "share",
                column: "borrower");

            migrationBuilder.CreateIndex(
                name: "IX_share_user_book_id",
                table: "share",
                column: "user_book_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "share");
        }
    }
}
