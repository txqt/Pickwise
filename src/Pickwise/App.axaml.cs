using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Pickwise.Services;
using Pickwise.ViewModels;
using Pickwise.Views;

namespace Pickwise;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var log = new LocalDiagnosticLog();
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainViewModel(new KuncLcuClient(log), log),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
