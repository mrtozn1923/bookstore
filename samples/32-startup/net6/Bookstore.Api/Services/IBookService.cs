using System.Collections.Generic;
using Bookstore.Api.Models;

namespace Bookstore.Api.Services
{
    public interface IBookService
    {
        IEnumerable<Book> GetAll();
        Book GetById(int id);
        bool Delete(int id);
    }
}
