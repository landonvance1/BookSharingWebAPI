using BookSharingApp.Models;

namespace BookSharingApp.Data
{
    public class MockDatabase
    {
        private List<Book> _books;
        private int _nextId;

        public MockDatabase()
        {
            _books = new List<Book>();
            _nextId = 1;
            SeedData();
        }

        public IEnumerable<Book> GetAllBooks()
        {
            return _books;
        }

        public Book? GetBookById(int id)
        {
            return _books.FirstOrDefault(b => b.Id == id);
        }

        public Book AddBook(Book book)
        {
            book.Id = _nextId++;
            _books.Add(book);
            return book;
        }

        public IEnumerable<Book> SearchBooks(string? search = null)
        {
            var query = _books.AsQueryable();
            query = query.Where(b => b.Title.Contains(search ?? string.Empty, StringComparison.OrdinalIgnoreCase) 
                             || b.Author.Contains(search ?? string.Empty, StringComparison.OrdinalIgnoreCase));

            return query.ToList();
        }

        private void SeedData()
        {
            _books.Add(new Book { Id = _nextId++, Title = "The Great Gatsby", Author = "F. Scott Fitzgerald", ISBN = "978-0-7432-7356-5", ThumbnailUrl = "/images/978-0-7432-7356-5.jpg" });
            _books.Add(new Book { Id = _nextId++, Title = "To Kill a Mockingbird", Author = "Harper Lee", ISBN = "978-0-06-112008-4", ThumbnailUrl = "/images/978-0-06-112008-4.jpg" });
            _books.Add(new Book { Id = _nextId++, Title = "1984", Author = "George Orwell", ISBN = "978-0-452-28423-4", ThumbnailUrl = "/images/notfound.jpg" });
            
            // Hilary Mantel
            _books.Add(new Book { Id = _nextId++, Title = "Wolf Hall", Author = "Hilary Mantel", ISBN = "978-0-8050-8068-6", ThumbnailUrl = "/images/978-0-8050-8068-6.jpg" });
            _books.Add(new Book { Id = _nextId++, Title = "Bring Up the Bodies", Author = "Hilary Mantel", ISBN = "978-0-8050-9049-4", ThumbnailUrl = "/images/978-0-8050-9049-4.jpg" });
            _books.Add(new Book { Id = _nextId++, Title = "The Mirror & the Light", Author = "Hilary Mantel", ISBN = "978-0-8050-9840-7", ThumbnailUrl = "/images/978-0-8050-9840-7.jpg" });
            _books.Add(new Book { Id = _nextId++, Title = "A Place of Greater Safety", Author = "Hilary Mantel", ISBN = "978-0-14-012419-0", ThumbnailUrl = "/images/978-0-14-012419-0.jpg" });
            
            // Joe Abercrombie
            _books.Add(new Book { Id = _nextId++, Title = "The Blade Itself", Author = "Joe Abercrombie", ISBN = "978-0-575-07972-5", ThumbnailUrl = "/images/978-0-575-07972-5.jpg" });
            _books.Add(new Book { Id = _nextId++, Title = "Before They Are Hanged", Author = "Joe Abercrombie", ISBN = "978-0-575-07973-2", ThumbnailUrl = "/images/978-0-575-07973-2.jpg" });
            _books.Add(new Book { Id = _nextId++, Title = "Last Argument of Kings", Author = "Joe Abercrombie", ISBN = "978-0-575-07974-9", ThumbnailUrl = "/images/978-0-575-07974-9.jpg" });
            _books.Add(new Book { Id = _nextId++, Title = "Best Served Cold", Author = "Joe Abercrombie", ISBN = "978-0-575-08311-1", ThumbnailUrl = "/images/978-0-575-08311-1.jpg" });
            
            // Ursula K Le Guin
            _books.Add(new Book { Id = _nextId++, Title = "The Left Hand of Darkness", Author = "Ursula K Le Guin", ISBN = "978-0-441-47812-5", ThumbnailUrl = "/images/978-0-441-47812-5.jpg" });
            _books.Add(new Book { Id = _nextId++, Title = "A Wizard of Earthsea", Author = "Ursula K Le Guin", ISBN = "978-0-553-38304-1", ThumbnailUrl = "/images/978-0-553-38304-1.jpg" });
            _books.Add(new Book { Id = _nextId++, Title = "The Dispossessed", Author = "Ursula K Le Guin", ISBN = "978-0-06-051275-5", ThumbnailUrl = "/images/978-0-06-051275-5.jpg" });
            _books.Add(new Book { Id = _nextId++, Title = "The Lathe of Heaven", Author = "Ursula K Le Guin", ISBN = "978-0-06-125901-3", ThumbnailUrl = "/images/notfound.jpg" });
            
            // Robin Hobb
            _books.Add(new Book { Id = _nextId++, Title = "Assassin's Apprentice", Author = "Robin Hobb", ISBN = "978-0-553-57339-4", ThumbnailUrl = "/images/978-0-553-57339-4.jpg" });
            _books.Add(new Book { Id = _nextId++, Title = "Royal Assassin", Author = "Robin Hobb", ISBN = "978-0-553-56440-8", ThumbnailUrl = "/images/978-0-553-56440-8.jpg" });
            _books.Add(new Book { Id = _nextId++, Title = "Assassin's Quest", Author = "Robin Hobb", ISBN = "978-0-553-56441-5", ThumbnailUrl = "/images/978-0-553-56441-5.jpg" });
            _books.Add(new Book { Id = _nextId++, Title = "Ship of Magic", Author = "Robin Hobb", ISBN = "978-0-553-56619-8", ThumbnailUrl = "/images/978-0-553-56619-8.jpg" });
            
            // Susanna Clarke
            _books.Add(new Book { Id = _nextId++, Title = "Jonathan Strange & Mr Norrell", Author = "Susanna Clarke", ISBN = "978-0-7475-8173-4", ThumbnailUrl = "/images/978-0-7475-8173-4.jpg" });
            _books.Add(new Book { Id = _nextId++, Title = "Piranesi", Author = "Susanna Clarke", ISBN = "978-1-63557-563-3", ThumbnailUrl = "/images/978-1-63557-563-3.jpg" });
            _books.Add(new Book { Id = _nextId++, Title = "The Ladies of Grace Adieu", Author = "Susanna Clarke", ISBN = "978-0-7475-8457-5", ThumbnailUrl = "/images/notfound.jpg" });
            _books.Add(new Book { Id = _nextId++, Title = "The Wood at Midwinter", Author = "Susanna Clarke", ISBN = "978-1-63557-982-2", ThumbnailUrl = "/images/notfound.jpg" });
        }
    }
}