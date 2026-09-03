namespace CrowdKeys.Services;

public static class MediaStorage
{
    private static readonly string MediaDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CrowdKeys", "Media");

    public static string CopyToLibrary(string sourcePath, string subfolder)
    {
        var dir = Path.Combine(MediaDir, subfolder);
        Directory.CreateDirectory(dir);

        var ext      = Path.GetExtension(sourcePath);
        var destPath = Path.Combine(dir, $"{Guid.NewGuid()}{ext}");

        File.Copy(sourcePath, destPath, overwrite: true);
        return destPath;
    }
}
