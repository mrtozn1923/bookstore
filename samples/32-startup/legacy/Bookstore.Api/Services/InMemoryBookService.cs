using System.Collections.Generic;
using System.Linq;
using Bookstore.Api.Models;

namespace Bookstore.Api.Services
{
    public class InMemoryBookService : IBookService
    {
        private readonly List<Book> _books = new List<Book>
        {
            new Book { Id = 1, Title = "Dune", Price = 450m },
            new Book { Id = 2, Title = "Emma", Price = 280m },
            new Book { Id = 3, Title = "The Hobbit", Price = 320m }
        };

        public IEnumerable<Book> GetAll() => _books;

        public Book GetById(int id) => _books.FirstOrDefault(b => b.Id == id);

        public bool Delete(int id) => _books.RemoveAll(b => b.Id == id) > 0;
    }
}
