using System.ComponentModel.DataAnnotations;

namespace Bookstore.Api.Dtos;

public class BookQueryParameters
{
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 50)]
    public int PageSize { get; set; } = 10;

    public string? Search { get; set; }
    public int? CategoryId { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }

    // allowed values: id, title, price
    public string? SortBy { get; set; }
    public bool Descending { get; set; }
}
