using System.Text.Json.Serialization;
using Bookstore.Api.Catalog;
using Bookstore.Api.Ordering;

namespace Bookstore.Api.Json;

// Derleme anında üretilen serializer; reflection yerine bu kod kullanılır
[JsonSerializable(typeof(IEnumerable<Book>))]
[JsonSerializable(typeof(OrderDto))]
[JsonSerializable(typeof(OrderDto[]))]
public partial class BookstoreJsonContext : JsonSerializerContext;
