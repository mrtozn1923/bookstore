using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Bookstore.Api.Data;
using Bookstore.Api.Dtos;
using Bookstore.Api.Models;

namespace Bookstore.Api.Services;

public class BookService : IBookService
{
    // a single projection EF Core can translate to SQL, reused by every query
    private static readonly Expression<Func<Book, BookDto>> ToDto =
        b => new BookDto(b.Id, b.Title, b.Price, b.Category!.Name);

    private readonly BookstoreContext _db;

    public BookService(BookstoreContext db)
    {
        _db = db;
    }

    public async Task<List<BookDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Books
            .AsNoTracking()
            .OrderBy(b => b.Id)
            .Select(ToDto)
            .ToListAsync(cancellationToken);
    }

    public async Task<BookDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _db.Books
            .AsNoTracking()
            .Where(b => b.Id == id)
            .Select(ToDto)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<BookDto?> CreateAsync(CreateBookRequest request, CancellationToken cancellationToken = default)
    {
        // business rule: no two books may share a title
        bool titleTaken = await _db.Books.AnyAsync(b => b.Title == request.Title, cancellationToken);
        if (titleTaken)
        {
            return null;
        }

        Book book = new()
        {
            Title = request.Title,
            Price = request.Price,
            CategoryId = request.CategoryId
        };

        _db.Books.Add(book);
        await _db.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(book.Id, cancellationToken);
    }

    public async Task<bool> UpdateAsync(int id, UpdateBookRequest request, CancellationToken cancellationToken = default)
    {
        Book? book = await _db.Books.FindAsync(new object[] { id }, cancellationToken);
        if (book is null)
        {
            return false;
        }

        book.Title = request.Title;
        book.Price = request.Price;
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        Book? book = await _db.Books.FindAsync(new object[] { id }, cancellationToken);
        if (book is null)
        {
            return false;
        }

        _db.Books.Remove(book);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
