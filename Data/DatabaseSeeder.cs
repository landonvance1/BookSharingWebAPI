using BookSharingApp.Models;
using Microsoft.EntityFrameworkCore;

namespace BookSharingApp.Data
{
    public static class DatabaseSeeder
    {
        public static async Task SeedAsync(ApplicationDbContext context)
        {
            await context.Database.EnsureCreatedAsync();
            
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
    }
}