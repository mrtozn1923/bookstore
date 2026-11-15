using System.Diagnostics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.Processing;

const string Assets = "../../tests/Bookstore.Notifications.Tests/Assets/";
Directory.CreateDirectory("out");

// Identify reads only the header
ImageInfo bomb = Image.Identify(Assets + "bomb.png");
Console.WriteLine($"bomb.png: {bomb.Width}x{bomb.Height} = {(long)bomb.Width * bomb.Height:N0} pixels");

using Image cover = Image.Load(Assets + "cover-dune.jpg");
ExifProfile exif = cover.Metadata.ExifProfile!;
Console.WriteLine($"cover-dune.jpg: {cover.Width}x{cover.Height}, {exif.Values.Count} EXIF values");
foreach (IExifValue value in exif.Values.Where(v => v.Tag == ExifTag.Orientation || v.Tag == ExifTag.Make || v.Tag == ExifTag.Model || v.Tag == ExifTag.GPSLatitude || v.Tag == ExifTag.GPSLongitude))
{
    object? shown = value.GetValue() is Rational[] r ? string.Join(" ", r.Select(x => x.ToDouble())) : value.GetValue();
    Console.WriteLine($"  {value.Tag}: {shown}");
}

Stopwatch watch = Stopwatch.StartNew();
using Image thumb = cover.Clone(x => x.AutoOrient().Resize(320, 0));
await thumb.SaveAsWebpAsync("out/dune-320-kept.webp", new WebpEncoder { Quality = 80 });
Console.WriteLine($"thumbnail {thumb.Width}x{thumb.Height} in {watch.ElapsedMilliseconds} ms, EXIF kept: {thumb.Metadata.ExifProfile?.Values.Count ?? 0} values, GPS={thumb.Metadata.ExifProfile?.TryGetValue(ExifTag.GPSLatitude, out _)}");

// Remove the metadata before saving
thumb.Metadata.ExifProfile = null;
thumb.Metadata.XmpProfile = null;
thumb.Metadata.IptcProfile = null;
await thumb.SaveAsWebpAsync("out/dune-320.webp", new WebpEncoder { Quality = 80 });
using Image check = Image.Load("out/dune-320.webp");
Console.WriteLine($"after clearing: {new FileInfo("out/dune-320.webp").Length:N0} bytes, EXIF={(check.Metadata.ExifProfile is null ? "none" : check.Metadata.ExifProfile.Values.Count + " values")}");
