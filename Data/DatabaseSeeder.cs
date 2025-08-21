using BookSharingApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BookSharingApp.Data
{
    public static class DatabaseSeeder
    {
        public static async Task SeedAsync(ApplicationDbContext context, UserManager<User> userManager)
        {
            await context.Database.EnsureCreatedAsync();

            // Seed users first
            await SeedUsersAsync(userManager);

            // Seed communities
            await SeedCommunitiesAsync(context);

            // Seed community users
            await SeedCommunityUsersAsync(context);

            // Seed books
            await SeedBooksAsync(context);
        }

        private static async Task SeedUsersAsync(UserManager<User> userManager)
        {
            // Check if users already exist
            if (userManager.Users.Any())
                return;

            var seedUsers = new List<(User user, string password)>
            {
                (new User
                {
                    Id = "user-001",
                    UserName = "l@v.com",
                    Email = "l@v.com",
                    FirstName = "Landon",
                    LastName = "Vance",
                    EmailConfirmed = true
                }, "password"),

                (new User
                {
                    Id = "user-002",
                    UserName = "john.doe@email.com",
                    Email = "john.doe@email.com",
                    FirstName = "John",
                    LastName = "Doe",
                    EmailConfirmed = true
                }, "password"),

                (new User
                {
                    Id = "user-003",
                    UserName = "jane.smith@email.com",
                    Email = "jane.smith@email.com",
                    FirstName = "Jane",
                    LastName = "Smith",
                    EmailConfirmed = true
                }, "password"),

                (new User
                {
                    Id = "user-004",
                    UserName = "bob.wilson@email.com",
                    Email = "bob.wilson@email.com",
                    FirstName = "Bob",
                    LastName = "Wilson",
                    EmailConfirmed = true
                }, "password"),

                (new User
                {
                    Id = "user-005",
                    UserName = "alice.brown@email.com",
                    Email = "alice.brown@email.com",
                    FirstName = "Alice",
                    LastName = "Brown",
                    EmailConfirmed = true
                }, "password")
            };

            foreach (var (user, password) in seedUsers)
            {
                var existingUser = await userManager.FindByEmailAsync(user.Email!);
                if (existingUser == null)
                {
                    var result = await userManager.CreateAsync(user, password);
                    if (!result.Succeeded)
                    {
                        throw new Exception($"Failed to create user {user.Email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    }
                }
            }
        }

        private static async Task SeedBooksAsync(ApplicationDbContext context)
        {
            if (await context.Books.AnyAsync())
                return; // Database has been seeded

            var books = new List<Book>
            {
                new Book { Title = "The Great Gatsby", Author = "F. Scott Fitzgerald", ISBN = "978-0-7432-7356-5" },
                new Book { Title = "To Kill a Mockingbird", Author = "Harper Lee", ISBN = "978-0-06-112008-4" },
                new Book { Title = "1984", Author = "George Orwell", ISBN = "978-0-452-28423-4" },
                
                // Hilary Mantel
                new Book { Title = "Wolf Hall", Author = "Hilary Mantel", ISBN = "978-0-8050-8068-6" },
                new Book { Title = "Bring Up the Bodies", Author = "Hilary Mantel", ISBN = "978-0-8050-9049-4" },
                new Book { Title = "The Mirror & the Light", Author = "Hilary Mantel", ISBN = "978-0-8050-9840-7" },
                new Book { Title = "A Place of Greater Safety", Author = "Hilary Mantel", ISBN = "978-0-14-012419-0" },
                
                // Joe Abercrombie
                new Book { Title = "The Blade Itself", Author = "Joe Abercrombie", ISBN = "978-0-575-07972-5" },
                new Book { Title = "Before They Are Hanged", Author = "Joe Abercrombie", ISBN = "978-0-575-07973-2" },
                new Book { Title = "Last Argument of Kings", Author = "Joe Abercrombie", ISBN = "978-0-575-07974-9" },
                new Book { Title = "Best Served Cold", Author = "Joe Abercrombie", ISBN = "978-0-575-08311-1" },
                
                // Ursula K Le Guin
                new Book { Title = "The Left Hand of Darkness", Author = "Ursula K Le Guin", ISBN = "978-0-441-47812-5" },
                new Book { Title = "A Wizard of Earthsea", Author = "Ursula K Le Guin", ISBN = "978-0-553-38304-1" },
                new Book { Title = "The Dispossessed", Author = "Ursula K Le Guin", ISBN = "978-0-06-051275-5" },
                new Book { Title = "The Lathe of Heaven", Author = "Ursula K Le Guin", ISBN = "978-0-06-125901-3" },
                
                // Robin Hobb
                new Book { Title = "Assassin's Apprentice", Author = "Robin Hobb", ISBN = "978-0-553-57339-4" },
                new Book { Title = "Royal Assassin", Author = "Robin Hobb", ISBN = "978-0-553-56440-8" },
                new Book { Title = "Assassin's Quest", Author = "Robin Hobb", ISBN = "978-0-553-56441-5" },
                new Book { Title = "Ship of Magic", Author = "Robin Hobb", ISBN = "978-0-553-56619-8" },
                
                // Susanna Clarke
                new Book { Title = "Jonathan Strange & Mr Norrell", Author = "Susanna Clarke", ISBN = "978-0-7475-8173-4" },
                new Book { Title = "Piranesi", Author = "Susanna Clarke", ISBN = "978-1-63557-563-3" },
                new Book { Title = "The Ladies of Grace Adieu", Author = "Susanna Clarke", ISBN = "978-0-7475-8457-5" },
                new Book { Title = "The Wood at Midwinter", Author = "Susanna Clarke", ISBN = "978-1-63557-982-2" }
            };

            context.Books.AddRange(books);
            await context.SaveChangesAsync();
        }

        private static async Task SeedCommunitiesAsync(ApplicationDbContext context)
        {
            if (await context.Communities.AnyAsync())
                return; // Database has been seeded

            var communities = new List<Community>
            {
                new Community { Id = 1, Name = "The Loop", Active = true },
                new Community { Id = 2, Name = "Cornerstone", Active = true },
                new Community { Id = 3, Name = "Tower Grove South", Active = true },
                new Community { Id = 4, Name = "Inactive Group", Active = false },
                new Community { Id = 5, Name = "DnD Boys", Active = true }
            };

            context.Communities.AddRange(communities);
            await context.SaveChangesAsync();
        }

        private static async Task SeedCommunityUsersAsync(ApplicationDbContext context)
        {
            if (await context.CommunityUsers.AnyAsync())
                return; // Database has been seeded

            var communityUsers = new List<CommunityUser>
            {
                // The Loop community members
                new CommunityUser { CommunityId = 1, UserId = "user-001" }, // Landon
                new CommunityUser { CommunityId = 1, UserId = "user-002" }, // John
                new CommunityUser { CommunityId = 1, UserId = "user-003" }, // Jane

                // Cornerstone community members
                new CommunityUser { CommunityId = 2, UserId = "user-001" }, // Landon
                new CommunityUser { CommunityId = 2, UserId = "user-004" }, // Bob
                new CommunityUser { CommunityId = 2, UserId = "user-005" }, // Alice

                // Tower Grove South community members
                new CommunityUser { CommunityId = 3, UserId = "user-002" }, // John
                new CommunityUser { CommunityId = 3, UserId = "user-003" }, // Jane
                new CommunityUser { CommunityId = 3, UserId = "user-004" }, // Bob

                // Inactive Group community members
                new CommunityUser { CommunityId = 4, UserId = "user-001" }, // Landon

                // DnD Boys community members
                new CommunityUser { CommunityId = 5, UserId = "user-001" }, // Landon
                new CommunityUser { CommunityId = 5, UserId = "user-002" }, // John
                new CommunityUser { CommunityId = 5, UserId = "user-004" }  // Bob
            };

            context.CommunityUsers.AddRange(communityUsers);
            await context.SaveChangesAsync();
        }
    }
}