using System.Text;
using Bookstore.Api.Uploads;
using Microsoft.Extensions.Options;

namespace Bookstore.Api.Tests;

public sealed class InspectionTests(ITestOutputHelper output)
{
    [Fact]
    public void Signature_decides_the_format_not_the_name()
    {
        (string Name, byte[] Content, UploadPurpose Purpose, bool Allowed)[] cases =
        [
            ("dune-cover.jpg", TestFiles.Jpeg(), UploadPurpose.Cover, true),
            ("dune-cover.jpg", TestFiles.Svg(), UploadPurpose.Cover, false),
            ("dune-cover.jpg", TestFiles.Polyglot(), UploadPurpose.Cover, true),
            ("dune-sample.pdf", TestFiles.Pdf(), UploadPurpose.SampleChapter, true),
            ("dune-sample.epub", TestFiles.Epub(("OEBPS/ch1.xhtml", TestFiles.Chapter())), UploadPurpose.SampleChapter, true),
            ("dune-sample.pdf", TestFiles.Jpeg(), UploadPurpose.SampleChapter, false),
        ];

        foreach ((string name, byte[] content, UploadPurpose purpose, bool allowed) in cases)
        {
            FileFormat? format = FileSignatures.Detect(content);
            bool accepted = format is { } f && FileSignatures.IsAllowed(purpose, f);
            output.WriteLine($"{name,-17} {purpose,-13} detected={format?.ToString() ?? "unknown",-8} accepted={accepted}");
            Assert.Equal(allowed, accepted);
        }
    }

    [Fact]
    public void Reencoding_drops_what_follows_the_image()
    {
        byte[] polyglot = TestFiles.Polyglot();
        byte[] cover = new CoverReencoder(Options.Create(new UploadOptions())).Reencode(polyglot);

        output.WriteLine($"input : {polyglot.Length:N0} bytes, {FileSignatures.Detect(polyglot)}, contains <script: {Contains(polyglot, "<script")}");
        output.WriteLine($"output: {cover.Length:N0} bytes, {FileSignatures.Detect(cover)}, contains <script: {Contains(cover, "<script")}");
        Assert.Equal(FileFormat.WebP, FileSignatures.Detect(cover));
        Assert.False(Contains(cover, "<script"));
    }

    [Fact]
    public async Task Archive_limits_stop_bombs_traversal_and_nesting()
    {
        // About 100 MB of zeros: compresses to a few hundred KB, stays in memory, never touches the disk
        byte[] zeros = new byte[100 * 1024 * 1024];
        (string Label, byte[] Epub, UploadOptions Options)[] cases =
        [
            ("valid", TestFiles.Epub(("OEBPS/ch1.xhtml", TestFiles.Chapter())), new UploadOptions()),
            ("bomb", TestFiles.Epub(("OEBPS/ch1.xhtml", zeros)), new UploadOptions()),
            ("bomb, ratio off", TestFiles.Epub(("OEBPS/ch1.xhtml", zeros)), new UploadOptions { MaxCompressionRatio = long.MaxValue }),
            ("traversal", TestFiles.Epub(("../../wwwroot/index.html", TestFiles.Chapter())), new UploadOptions()),
            ("nested", TestFiles.Epub(("OEBPS/extra.zip", TestFiles.Epub())), new UploadOptions()),
        ];

        foreach ((string label, byte[] epub, UploadOptions options) in cases)
        {
            string? problem = await new EpubInspector(Options.Create(options))
                .FindProblemAsync(new MemoryStream(epub), TestContext.Current.CancellationToken);
            output.WriteLine($"{label,-16} {epub.Length,9:N0} bytes  {problem ?? "OK"}");
            Assert.Equal(label == "valid", problem is null);
        }
    }

    [Fact]
    public void Display_name_keeps_no_path_and_no_control_characters()
    {
        string name = UploadFileName.ForDisplay("..\\..\\appsettings.json\r\n[INF] Admin login ok");
        output.WriteLine(name);
        Assert.Equal("appsettings.json[INF] Admin login ok", name);
    }

    private static bool Contains(byte[] data, string text) => data.AsSpan().IndexOf(Encoding.ASCII.GetBytes(text)) >= 0;
}
