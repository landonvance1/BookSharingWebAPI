using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BookSharingApp.Migrations
{
    /// <inheritdoc />
    public partial class AddChatTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "chat_thread",
                columns: table => new
                {
                    thread_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_activity = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chat_thread", x => x.thread_id);
                });

            migrationBuilder.CreateTable(
                name: "chat_message",
                columns: table => new
                {
                    message_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    thread_id = table.Column<int>(type: "integer", nullable: false),
                    sender_id = table.Column<string>(type: "text", nullable: false),
                    content = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_system_message = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chat_message", x => x.message_id);
                    table.ForeignKey(
                        name: "FK_chat_message_AspNetUsers_sender_id",
                        column: x => x.sender_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_chat_message_chat_thread_thread_id",
                        column: x => x.thread_id,
                        principalTable: "chat_thread",
                        principalColumn: "thread_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "share_chat_thread",
                columns: table => new
                {
                    thread_id = table.Column<int>(type: "integer", nullable: false),
                    share_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_share_chat_thread", x => x.thread_id);
                    table.ForeignKey(
                        name: "FK_share_chat_thread_chat_thread_thread_id",
                        column: x => x.thread_id,
                        principalTable: "chat_thread",
                        principalColumn: "thread_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_share_chat_thread_share_share_id",
                        column: x => x.share_id,
                        principalTable: "share",
                        principalColumn: "share_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessage_SenderId",
                table: "chat_message",
                column: "sender_id");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessage_Thread_SentAt",
                table: "chat_message",
                columns: new[] { "thread_id", "sent_at" });

            migrationBuilder.CreateIndex(
                name: "IX_ShareChatThread_ShareId_Unique",
                table: "share_chat_thread",
                column: "share_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "chat_message");

            migrationBuilder.DropTable(
                name: "share_chat_thread");

            migrationBuilder.DropTable(
                name: "chat_thread");
        }
    }
}
