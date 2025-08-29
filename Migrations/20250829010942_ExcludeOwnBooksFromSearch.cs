using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookSharingApp.Migrations
{
    /// <inheritdoc />
    public partial class ExcludeOwnBooksFromSearch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE OR REPLACE FUNCTION search_accessible_books(
                    p_user_id TEXT,
                    p_search TEXT DEFAULT NULL
                )
                RETURNS TABLE(
                    book_id INTEGER,
                    title TEXT,
                    author TEXT,
                    isbn TEXT,
                    user_book_id INTEGER,
                    owner_user_id TEXT,
                    status INTEGER,
                    community_id INTEGER,
                    community_name TEXT
                ) AS $$
                BEGIN
                    RETURN QUERY
                    SELECT 
                        b.book_id,
                        b.title,
                        b.author,
                        b.isbn,
                        ub.user_book_id,
                        ub.user_id as owner_user_id,
                        ub.status,
                        c.community_id,
                        c.name as community_name
                    FROM book b
                    INNER JOIN user_book ub ON ub.book_id = b.book_id AND ub.status = 1 AND ub.user_id != p_user_id
                    INNER JOIN community_user cu ON cu.user_id = ub.user_id
                    INNER JOIN community c ON c.community_id = cu.community_id
                    WHERE c.community_id IN (
                        SELECT cu2.community_id 
                        FROM community_user cu2
                        WHERE cu2.user_id = p_user_id
                    )
                    AND (
                        p_search IS NULL 
                        OR p_search = '' 
                        OR b.title ILIKE '%' || p_search || '%' 
                        OR b.author ILIKE '%' || p_search || '%'
                    )
                    ORDER BY b.title, c.name;
                END;
                $$ LANGUAGE plpgsql;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
