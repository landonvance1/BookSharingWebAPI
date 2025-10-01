using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookSharingApp.Migrations
{
    /// <inheritdoc />
    public partial class AddSystemUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Insert system user with NULL password hash to prevent login
            migrationBuilder.Sql(@"
                INSERT INTO ""AspNetUsers""
                (""Id"", ""UserName"", ""NormalizedUserName"", ""Email"", ""NormalizedEmail"",
                 ""EmailConfirmed"", ""PasswordHash"", ""SecurityStamp"", ""ConcurrencyStamp"",
                 ""PhoneNumberConfirmed"", ""TwoFactorEnabled"", ""LockoutEnabled"",
                 ""AccessFailedCount"", ""first_name"", ""last_name"")
                VALUES
                ('system', 'system@booksharing.internal', 'SYSTEM@BOOKSHARING.INTERNAL',
                 'system@booksharing.internal', 'SYSTEM@BOOKSHARING.INTERNAL',
                 true, NULL,
                 REPLACE(gen_random_uuid()::text, '-', ''), REPLACE(gen_random_uuid()::text, '-', ''),
                 false, false, false, 0, 'System', 'User')
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM ""AspNetUsers"" WHERE ""Id"" = 'system'
            ");
        }
    }
}
