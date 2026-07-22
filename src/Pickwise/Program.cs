using Avalonia;
using Pickwise.Services;
using System;

namespace Pickwise;

sealed class Program
{
    private static readonly LocalDiagnosticLog Log = new();

    [STAThread]
    public static void Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is Exception exception)
            {
                Log.Crash("Unhandled AppDomain exception", exception);
            }
        };

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            Log.Crash("Unobserved task exception", e.Exception);
            e.SetObserved();
        };

        try
        {
            Log.Info("Pickwise starting");
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            Log.Info("Pickwise exited");
        }
        catch (Exception exception)
        {
            Log.Crash("Fatal startup exception", exception);
            Environment.ExitCode = 1;
        }
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
