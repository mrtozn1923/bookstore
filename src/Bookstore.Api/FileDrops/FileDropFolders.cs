namespace Bookstore.FileDrops;

// Local working folders: <root>/<feed>/incoming|processing|processed|failed and reports.
public sealed class FileDropFolders(string root)
{
    public string Path(string feed, string stage, string fileName)
    {
        var folder = System.IO.Path.Combine(root, feed, stage);
        Directory.CreateDirectory(folder);
        return System.IO.Path.Combine(folder, fileName);
    }
}
