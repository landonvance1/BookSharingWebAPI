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
            _books.Add(new Book { Id = _nextId++, Title = "The Great Gatsby", Author = "F. Scott Fitzgerald", ISBN = "978-0-7432-7356-5" });
            _books.Add(new Book { Id = _nextId++, Title = "To Kill a Mockingbird", Author = "Harper Lee", ISBN = "978-0-06-112008-4" });
            _books.Add(new Book { Id = _nextId++, Title = "1984", Author = "George Orwell", ISBN = "978-0-452-28423-4" });
        }
    }
}