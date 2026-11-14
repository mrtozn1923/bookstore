using Bookstore.Api.Dtos;

namespace Bookstore.Api.Services;

public interface IBookService
{
    Task<List<BookDto>> GetAllAsync();
    Task<BookDto?> GetByIdAsync(int id);
    Task<BookDto?> CreateAsync(CreateBookRequest request);
    Task<bool> UpdateAsync(int id, UpdateBookRequest request);
    Task<bool> DeleteAsync(int id);
}
