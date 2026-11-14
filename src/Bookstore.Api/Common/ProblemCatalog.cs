using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Bookstore.Api.Common;

// One entry per problem type. The type URI and code never change; title and detail are
// human-readable and localized (en, tr). Detail templates take the occurrence's values.
public sealed record ProblemType(
    string Code, int Status, string TitleEn, string TitleTr, string DetailEn, string DetailTr)
{
    public string Type => ProblemCatalog.BaseUri + Code;
}

public static class ProblemCatalog
{
    public const string BaseUri = "https://api.example.com/problems/";

    public static readonly ProblemType ValidationFailed = new("validation-failed", 400,
        "Some fields are invalid", "Bazı alanlar geçersiz",
        "Fix the fields listed in errors.", "errors içindeki alanları düzelt.");
    public static readonly ProblemType MalformedRequest = new("malformed-request", 400,
        "Request body could not be read", "İstek gövdesi okunamadı",
        "The body is not valid JSON or a value has the wrong type.",
        "Gövde geçerli bir JSON değil ya da bir değerin tipi yanlış.");
    public static readonly ProblemType Unauthenticated = new("unauthenticated", 401,
        "Authentication required", "Kimlik doğrulaması gerekiyor",
        "Send a valid bearer token.", "Geçerli bir bearer token gönder.");
    public static readonly ProblemType Forbidden = new("forbidden", 403,
        "Not allowed", "İzin yok",
        "You are not allowed to do this.", "Bu işlem için yetkin yok.");
    public static readonly ProblemType UnitNotAllowed = new("unit-not-allowed", 403,
        "Unit not allowed", "Bu birime erişim yok",
        "Buyers can only list the orders of their own unit ({0}).",
        "Alıcılar yalnız kendi birimlerinin ({0}) siparişlerini listeleyebilir.");
    public static readonly ProblemType NotFound = new("not-found", 404,
        "Not found", "Bulunamadı",
        "Nothing exists at {0}.", "{0} adresinde bir kayıt yok.");
    public static readonly ProblemType MethodNotAllowed = new("method-not-allowed", 405,
        "Method not allowed", "Method desteklenmiyor",
        "See the Allow header for the supported methods.", "Desteklenen method'lar Allow header'ında.");
    public static readonly ProblemType BookAlreadyExists = new("book-already-exists", 409,
        "Book already exists", "Kitap zaten var",
        "A book with ISBN {0} already exists; use PUT /books/{0} to replace it.",
        "{0} ISBN'li kitap zaten var; değiştirmek için PUT /books/{0} kullan.");
    public static readonly ProblemType OrderNotCancellable = new("order-not-cancellable", 409,
        "Order cannot be cancelled", "Sipariş iptal edilemez",
        "Order {0} is {1}; only Pending or Confirmed orders can be cancelled.",
        "{0} siparişi {1} durumunda; yalnız Pending ya da Confirmed siparişler iptal edilebilir.");
    public static readonly ProblemType OrderNotReturnable = new("order-not-returnable", 409,
        "Order cannot be returned", "Sipariş iade edilemez",
        "Order {0} is {1}; only Delivered orders can be returned.",
        "{0} siparişi {1} durumunda; yalnız Delivered siparişler iade edilebilir.");
    public static readonly ProblemType PreconditionFailed = new("precondition-failed", 412,
        "Precondition failed", "Ön koşul tutmadı",
        "If-Match {0} does not match the current ETag {1}.", "If-Match {0}, güncel ETag {1} ile eşleşmiyor.");
    public static readonly ProblemType UnsupportedMediaType = new("unsupported-media-type", 415,
        "Unsupported media type", "Desteklenmeyen içerik tipi",
        "This endpoint accepts {0}.", "Bu endpoint yalnız {0} kabul ediyor.");
    public static readonly ProblemType InsufficientStock = new("insufficient-stock", 422,
        "Insufficient stock", "Stok yetersiz",
        "Only {0} copies of {1} are available.", "{1} kitabından yalnızca {0} adet var.");
    public static readonly ProblemType UnsupportedCurrency = new("unsupported-currency", 422,
        "Unsupported currency", "Desteklenmeyen para birimi",
        "Catalog prices are in TRY; {0} is not accepted.", "Katalog fiyatları TRY; {0} kabul edilmiyor.");
    public static readonly ProblemType InternalError = new("internal-error", 500,
        "Internal error", "Sunucu hatası",
        "Something went wrong on our side. Quote traceId when you contact support.",
        "Bizim tarafımızda bir hata oluştu. Destek ekibine yazarken traceId'yi belirt.");

    public static readonly IReadOnlyList<ProblemType> All =
    [
        ValidationFailed, MalformedRequest, Unauthenticated, Forbidden, UnitNotAllowed, NotFound, MethodNotAllowed,
        BookAlreadyExists, OrderNotCancellable, OrderNotReturnable, PreconditionFailed, UnsupportedMediaType,
        InsufficientStock, UnsupportedCurrency, InternalError,
    ];

    private static bool Turkish => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "tr";

    public static string Title(ProblemType type) => Turkish ? type.TitleTr : type.TitleEn;

    public static string Detail(ProblemType type, params object?[] args) =>
        string.Format(CultureInfo.CurrentCulture, Turkish ? type.DetailTr : type.DetailEn, args);

    public static ProblemHttpResult Create(
        ProblemType type, object?[]? args = null, Dictionary<string, object?>? extensions = null) =>
        TypedResults.Problem(statusCode: type.Status, type: type.Type, title: Title(type),
            detail: Detail(type, args ?? []), extensions: extensions);

    // Framework-generated problems (routing, auth, binding, validation, exceptions) get catalog types too.
    public static void Normalize(ProblemDetailsContext context)
    {
        ProblemDetails problem = context.ProblemDetails;
        HttpContext http = context.HttpContext;
        problem.Instance = http.Request.Path;
        http.Response.Headers.Vary = "Accept-Language";

        if (problem.Type?.StartsWith(BaseUri, StringComparison.Ordinal) == true)
        {
            return;
        }

        ProblemType? type = problem.Status switch
        {
            400 when problem is HttpValidationProblemDetails { Errors.Count: > 0 } => ValidationFailed,
            400 => MalformedRequest,
            401 => Unauthenticated,
            403 => Forbidden,
            404 => NotFound,
            405 => MethodNotAllowed,
            415 => UnsupportedMediaType,
            >= 500 => InternalError,
            _ => null,
        };
        if (type is null)
        {
            return;
        }

        problem.Type = type.Type;
        problem.Title = Title(type);
        problem.Detail = type == NotFound ? Detail(type, http.Request.Path.Value)
            : type == UnsupportedMediaType ? Detail(type, "application/json")
            : Detail(type);

        if (problem is HttpValidationProblemDetails validation)
        {
            // "Price.Amount" -> "price.amount": keys follow the JSON names of the contract.
            Dictionary<string, string[]> errors = validation.Errors.ToDictionary(
                e => string.Join('.', e.Key.Split('.').Select(JsonNamingPolicy.CamelCase.ConvertName)), e => e.Value);
            validation.Errors.Clear();
            foreach ((string key, string[] value) in errors)
            {
                validation.Errors[key] = value;
            }
        }
    }
}
