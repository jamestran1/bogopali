using Avalonia;
using Avalonia.Controls;

namespace Bogopali.App;

internal static class Program
{
    // "Local\" scopes the name to the user's session, so each signed-in user can run one instance.
    private const string SingleInstanceName = @"Local\bogopali.single-instance";

    [STAThread]
    public static int Main(string[] args)
    {
        // A second instance would install a second hook and cause double Replacements.
        using Mutex singleInstance = new(initiallyOwned: true, SingleInstanceName, out bool isFirstInstance);
        if (!isFirstInstance)
        {
            return 0;
        }

        return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args, ShutdownMode.OnExplicitShutdown);
    }

    // Used by the Avalonia previewer as well as Main.
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
}
