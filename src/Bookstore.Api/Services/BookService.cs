using System.Linq.Expressions;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Bookstore.Api.Data;
using Bookstore.Api.Dtos;
using Bookstore.Api.Exceptions;
using Bookstore.Api.Models;

namespace Bookstore.Api.Services;

public class BookService : IBookService
{
    // a single projection EF Core can translate to SQL, reused by every query
    private static readonly Expression<Func<Book, BookDto>> ToDto =
        b => new BookDto(b.Id, b.Title, b.Price, b.Category!.Name);

    private readonly BookstoreContext _db;
    private readonly IDistributedCache _cache;

    public BookService(BookstoreContext db, IDistributedCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<List<BookDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Books
            .AsNoTracking()
            .OrderBy(b => b.Id)
            .Select(ToDto)
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<BookDto>> GetPagedAsync(
        BookQueryParameters parameters, CancellationToken cancellationToken = default)
    {
        IQueryable<Book> query = _db.Books.AsNoTracking();

        // 1. filtering: each condition is added to the query only if its parameter was sent
        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            query = query.Where(b => b.Title.Contains(parameters.Search));
        }

        if (parameters.CategoryId is not null)
        {
            query = query.Where(b => b.CategoryId == parameters.CategoryId);
        }

        if (parameters.MinPrice is not null)
        {
            query = query.Where(b => b.Price >= parameters.MinPrice);
        }

        if (parameters.MaxPrice is not null)
        {
            query = query.Where(b => b.Price <= parameters.MaxPrice);
        }

        // 2. total count: after the filters, before pagination
        int totalCount = await query.CountAsync(cancellationToken);

        // 3. sorting and pagination
        List<BookDto> items = await ApplySorting(query, parameters.SortBy, parameters.Descending)
            .Skip((parameters.Page - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .Select(ToDto)
            .ToListAsync(cancellationToken);

        return new PagedResult<BookDto>(items, parameters.Page, parameters.PageSize, totalCount);
    }

    private static IQueryable<Book> ApplySorting(IQueryable<Book> query, string? sortBy, bool descending)
    {
        IOrderedQueryable<Book> ordered = (sortBy?.ToLowerInvariant(), descending) switch
        {
            ("title", false) => query.OrderBy(b => b.Title),
            ("title", true) => query.OrderByDescending(b => b.Title),
            // SQLite + EF Core 6 can't ORDER BY a decimal; we sort by casting to double
            ("price", false) => query.OrderBy(b => (double)b.Price),
            ("price", true) => query.OrderByDescending(b => (double)b.Price),
            (_, false) => query.OrderBy(b => b.Id),
            (_, true) => query.OrderByDescending(b => b.Id)
        };

        // a second sort by Id keeps equal values in the same order in every query
        return ordered.ThenBy(b => b.Id);
    }

    public async Task<BookDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        string key = CacheKey(id);

        string? json = await _cache.GetStringAsync(key, cancellationToken);
        if (json is not null)
        {
            return JsonSerializer.Deserialize<BookDto>(json)!;
        }

        BookDto? book = await _db.Books
            .AsNoTracking()
            .Where(b => b.Id == id)
            .Select(ToDto)
            .FirstOrDefaultAsync(cancellationToken);

        if (book is null)
        {
            throw new NotFoundException("Book", id);
        }

        await _cache.SetStringAsync(key, JsonSerializer.Serialize(book), new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
        }, cancellationToken);

        return book;
    }

    // the only place that builds the key
    private static string CacheKey(int id) => $"book:{id}";

    public async Task<BookDto> CreateAsync(CreateBookRequest request, CancellationToken cancellationToken = default)
    {
        bool titleTaken = await _db.Books.AnyAsync(b => b.Title == request.Title, cancellationToken);
        if (titleTaken)
        {
            throw new ConflictException($"A book titled '{request.Title}' already exists.");
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

        // the data changed, throw away the old copy
        await _cache.RemoveAsync(CacheKey(id), cancellationToken);
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
        await _cache.RemoveAsync(CacheKey(id), cancellationToken);
        return true;
    }
}
