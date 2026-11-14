using System.ComponentModel.DataAnnotations;

namespace Bookstore.Api.Dtos;

public record DiscountRequest([Range(0.01, 1000)] decimal Amount);
