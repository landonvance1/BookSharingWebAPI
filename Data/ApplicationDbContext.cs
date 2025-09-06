using BookSharingApp.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BookSharingApp.Data
{
    public class ApplicationDbContext : IdentityDbContext<User>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Book> Books { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<Community> Communities { get; set; }
        public DbSet<CommunityUser> CommunityUsers { get; set; }
        public DbSet<UserBook> UserBooks { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            modelBuilder.Entity<User>(entity =>
            {
                entity.Property(e => e.FirstName).HasColumnName("first_name").IsRequired();
                entity.Property(e => e.LastName).HasColumnName("last_name").IsRequired();
            });

            modelBuilder.Entity<Book>(entity =>
            {
                entity.ToTable("book");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("book_id").ValueGeneratedOnAdd();
                entity.Property(e => e.Title).HasColumnName("title").IsRequired();
                entity.Property(e => e.Author).HasColumnName("author").IsRequired();
                entity.HasIndex(e => new { e.Title, e.Author }).IsUnique();
                entity.Ignore(e => e.ThumbnailUrl);
                entity.Ignore(e => e.ExternalThumbnailUrl);
            });

            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.ToTable("refresh_tokens");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
                entity.Property(e => e.Token).HasColumnName("token").IsRequired();
                entity.Property(e => e.UserId).HasColumnName("user_id").IsRequired();
                entity.Property(e => e.ExpiresAt).HasColumnName("expires_at").IsRequired();
                entity.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
                entity.Property(e => e.RevokedAt).HasColumnName("revoked_at");
                entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId);
                entity.Ignore(e => e.IsExpired);
                entity.Ignore(e => e.IsRevoked);
                entity.Ignore(e => e.IsActive);
            });

            modelBuilder.Entity<Community>(entity =>
            {
                entity.ToTable("community");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("community_id").ValueGeneratedOnAdd();
                entity.Property(e => e.Name).HasColumnName("name").IsRequired();
                entity.Property(e => e.Active).HasColumnName("active").IsRequired();
            });

            modelBuilder.Entity<CommunityUser>(entity =>
            {
                entity.ToTable("community_user");
                entity.HasKey(e => new { e.CommunityId, e.UserId });
                entity.Property(e => e.CommunityId).HasColumnName("community_id").IsRequired();
                entity.Property(e => e.UserId).HasColumnName("user_id").IsRequired();
                entity.HasOne(e => e.Community).WithMany(c => c.Members).HasForeignKey(e => e.CommunityId);
                entity.HasOne(e => e.User).WithMany(u => u.JoinedCommunities).HasForeignKey(e => e.UserId);
            });

            modelBuilder.Entity<UserBook>(entity =>
            {
                entity.ToTable("user_book");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("user_book_id").ValueGeneratedOnAdd();
                entity.Property(e => e.UserId).HasColumnName("user_id").IsRequired();
                entity.Property(e => e.BookId).HasColumnName("book_id").IsRequired();
                entity.Property(e => e.Status).HasColumnName("status").IsRequired();
                entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId);
                entity.HasOne(e => e.Book).WithMany().HasForeignKey(e => e.BookId);
            });
        }
    }
}