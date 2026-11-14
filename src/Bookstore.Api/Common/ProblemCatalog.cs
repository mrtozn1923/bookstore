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
        "The body is not valid JSON, or a body or query value has the wrong type.",
        "Gövde geçerli bir JSON değil ya da gövdedeki veya query'deki bir değerin tipi yanlış.");
    public static readonly ProblemType UnsupportedApiVersion = new("unsupported-api-version", 400,
        "Unsupported api-version", "Desteklenmeyen api-version",
        "api-version {0} is not supported; see supportedVersions.",
        "{0} api-version'ı desteklenmiyor; desteklenenler supportedVersions içinde.");
    public static readonly ProblemType InvalidPageToken = new("invalid-page-token", 400,
        "Invalid page token", "Geçersiz sayfa token'ı",
        "The page token is invalid, has expired or belongs to a different query; start again without pageToken.",
        "Sayfa token'ı geçersiz, süresi dolmuş ya da başka bir sorguya ait; pageToken olmadan baştan başla.");
    public static readonly ProblemType Unauthenticated = new("unauthenticated", 401,
        "Authentication required", "Kimlik doğrulaması gerekiyor",
        "Send a valid bearer token.", "Geçerli bir bearer token gönder.");
    public static readonly ProblemType Forbidden = new("forbidden", 403,
        "Not allowed", "İzin yok",
        "You are not allowed to do this.", "Bu işlem için yetkin yok.");
    public static readonly ProblemType UnitNotAllowed = new("unit-not-allowed", 403,
        "Unit not allowed", "Bu birime erişim yok",
        "Buyers can only see and place the orders of their own unit ({0}).",
        "Alıcılar yalnız kendi birimlerinin ({0}) siparişlerini görebilir ve verebilir.");
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
    public static readonly ProblemType IdempotencyKeyMissing = new("idempotency-key-missing", 400,
        "Idempotency-Key is missing", "Idempotency-Key eksik",
        "This operation requires an Idempotency-Key header (a quoted string, e.g. a UUID); see the type URI.",
        "Bu işlem bir Idempotency-Key header'ı (tırnak içinde bir string, örneğin bir UUID) gerektiriyor; type URI'ına bak.");
    public static readonly ProblemType IdempotencyKeyInProgress = new("idempotency-key-in-progress", 409,
        "A request with this Idempotency-Key is in progress", "Bu Idempotency-Key ile bir istek işleniyor",
        "Wait for the first request to finish, then retry with the same key.",
        "İlk isteğin bitmesini bekle, sonra aynı anahtarla tekrar dene.");
    public static readonly ProblemType IdempotencyKeyReused = new("idempotency-key-reused", 422,
        "Idempotency-Key is already used", "Idempotency-Key zaten kullanılmış",
        "This Idempotency-Key was used with a different request body; use a new key for a new request.",
        "Bu Idempotency-Key farklı bir istek gövdesiyle kullanıldı; yeni bir istek için yeni bir anahtar kullan.");
    public static readonly ProblemType OrderNotInvoiceable = new("order-not-invoiceable", 409,
        "Order cannot be invoiced", "Sipariş faturalanamaz",
        "Order {0} is {1}; Pending and Cancelled orders have no invoice.",
        "{0} siparişi {1} durumunda; Pending ve Cancelled siparişlerin faturası olmaz.");
    public static readonly ProblemType InvoiceAlreadyRequested = new("invoice-already-requested", 409,
        "Invoice already requested", "Fatura zaten istendi",
        "An invoice for order {0} is being generated or exists; see operationLocation.",
        "{0} siparişinin faturası üretiliyor ya da hazır; operationLocation'a bak.");
    public static readonly ProblemType OperationNotCancellable = new("operation-not-cancellable", 409,
        "Operation cannot be cancelled", "Operation iptal edilemez",
        "Operation {0} has already ended with {1}.", "{0} operation'ı {1} durumuyla bitmiş.");
    public static readonly ProblemType PreconditionRequired = new("precondition-required", 428,
        "Precondition required", "Ön koşul gerekiyor",
        "Send If-Match with the ETag from your last GET.", "Son GET'teki ETag'i If-Match ile gönder.");
    public static readonly ProblemType InvoiceDetailsMissing = new("invoice-details-missing", 422,
        "Invoice details missing", "Fatura bilgileri eksik",
        "Institution {0} has no tax number on file; ask your account manager to add it.",
        "{0} kurumunun kayıtlı vergi numarası yok; müşteri temsilcinden eklemesini iste.");
    public static readonly ProblemType BatchRejected = new("batch-rejected", 422,
        "Batch rejected", "Toplu sipariş reddedildi",
        "No order was created: {0} of {1} lines were rejected; see results.",
        "Sipariş oluşturulmadı: {1} satırın {0} tanesi reddedildi; results'a bak.");
    public static readonly ProblemType QuotaExceeded = new("quota-exceeded", 429,
        "Quota exceeded", "Kota aşıldı",
        "Policy {0} allows {1} requests per {2} seconds; wait for Retry-After seconds.",
        "{0} politikası {2} saniyede {1} isteğe izin veriyor; Retry-After kadar bekle.");
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
        ValidationFailed, MalformedRequest, UnsupportedApiVersion, InvalidPageToken, Unauthenticated, Forbidden, UnitNotAllowed, NotFound, MethodNotAllowed,
        BookAlreadyExists, OrderNotCancellable, OrderNotReturnable, OrderNotInvoiceable, InvoiceAlreadyRequested,
        OperationNotCancellable, IdempotencyKeyMissing, IdempotencyKeyInProgress, IdempotencyKeyReused, PreconditionFailed,
        PreconditionRequired, UnsupportedMediaType, InvoiceDetailsMissing,
        BatchRejected, QuotaExceeded, InsufficientStock, UnsupportedCurrency, InternalError,
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
            428 => PreconditionRequired,
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
