using Windows.Storage;

namespace Bogopali.Windows;

/// <summary>Facts about how bogopali was installed: from the MSIX package (Store) or the portable ZIP.</summary>
public static class WindowsPackage
{
    private static readonly Lazy<bool> s_isPackaged = new(DetectPackaged);

    /// <summary>Whether bogopali runs from an MSIX package.</summary>
    public static bool IsPackaged => s_isPackaged.Value;

    /// <summary>
    /// The folder that holds <c>config.toml</c>: <c>%LOCALAPPDATA%\bogopali</c>. In the MSIX build, writes to
    /// that folder are redirected into the package's private <c>LocalCache\Local</c> folder, so the real path
    /// is returned there; otherwise an external text editor would look in the unredirected folder.
    /// </summary>
    /// <returns>The absolute folder path.</returns>
    public static string GetConfigDirectory()
    {
        if (IsPackaged)
        {
            return Path.Combine(ApplicationData.Current.LocalCacheFolder.Path, "Local", "bogopali");
        }

        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.Create),
            "bogopali");
    }

    private static unsafe bool DetectPackaged()
    {
        uint length = 0;
        int result = NativeMethods.GetCurrentPackageFullName(ref length, null);
        return result != NativeMethods.AppModelErrorNoPackage;
    }
}
