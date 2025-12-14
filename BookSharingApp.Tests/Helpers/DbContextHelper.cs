using BookSharingApp.Data;
using Microsoft.EntityFrameworkCore;

namespace BookSharingApp.Tests.Helpers
{
    public static class DbContextHelper
    {
        public static ApplicationDbContext CreateInMemoryContext()
        {
            // Use a unique database name for each test to ensure isolation
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var context = new ApplicationDbContext(options);

            // Ensure the database is created
            context.Database.EnsureCreated();

            return context;
        }

        public static ApplicationDbContext CreateInMemoryContext(string databaseName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: databaseName)
                .Options;

            var context = new ApplicationDbContext(options);

            // Ensure the database is created
            context.Database.EnsureCreated();

            return context;
        }
    }
}
