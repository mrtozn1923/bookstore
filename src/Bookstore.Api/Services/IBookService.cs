using Bookstore.Api.Dtos;

namespace Bookstore.Api.Services;

public interface IBookService
{
    Task<List<BookDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<BookDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<BookDto> CreateAsync(CreateBookRequest request, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(int id, UpdateBookRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
