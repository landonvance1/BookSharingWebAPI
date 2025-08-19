using BookSharingApp.Models;
using Microsoft.EntityFrameworkCore;

namespace BookSharingApp.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Book> Books { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Book>(entity =>
            {
                entity.ToTable("book");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("book_id").ValueGeneratedOnAdd();
                entity.Property(e => e.Title).HasColumnName("title").IsRequired();
                entity.Property(e => e.Author).HasColumnName("author").IsRequired();
                entity.Property(e => e.ISBN).HasColumnName("isbn").IsRequired();
                entity.Ignore(e => e.ThumbnailUrl);
            });
        }
    }
}