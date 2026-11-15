using Bookstore.Web.Catalog;
using Bookstore.Web.Urls;

namespace Bookstore.Web.Images;

public static class CoverImages
{
    // kapaklar 2:3; 1200 px sürüm og:image ve büyük önizleme için
    public static readonly int[] Widths = [320, 640, 1200];

    // dosya adı dilden bağımsız: kitap + yazar, ör. dune-frank-herbert-640.jpg
    public static string FileStem(Book book) => $"{book.Slug}-{Slugifier.ToSlug(book.Author)}";

    public static string Url(Book book, int width, string ext, string baseUrl) =>
        $"{baseUrl}/images/covers/{FileStem(book)}-{width}.{ext}";

    public static string SrcSet(Book book, string ext, string baseUrl) =>
        string.Join(", ", Widths.Select(w => $"{Url(book, w, ext, baseUrl)} {w}w"));

    // alt metni katalog verisinden: başlık, yazar, baskı türü
    public static string Alt(Book book, string lang) => lang == "tr"
        ? $"{book.Title}, {book.Author}, {FormatTr(book.Format)} kapağı"
        : $"Cover of {book.Title} by {book.Author}, {FormatEn(book.Format)}";

    private static string FormatTr(BookFormat format) => format switch
    {
        BookFormat.Hardcover => "ciltli baskı",
        BookFormat.Paperback => "karton kapaklı baskı",
        _ => "e-kitap",
    };

    private static string FormatEn(BookFormat format) => format switch
    {
        BookFormat.Hardcover => "hardcover edition",
        BookFormat.Paperback => "paperback edition",
        _ => "ebook edition",
    };
}
