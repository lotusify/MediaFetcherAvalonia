using Avalonia;
using System;
using System.Runtime.InteropServices;

namespace MediaFetcherAvalonia;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
    {
        var builder = AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();

        // Add Windows-specific configuration
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            builder.UseWin32();
        }

        return builder;
    }
}
