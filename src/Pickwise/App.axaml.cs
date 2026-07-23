using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Pickwise.Models;
using Pickwise.Services;
using Pickwise.ViewModels;
using Pickwise.Views;

namespace Pickwise;

public partial class App : Application
{
    private const string NormalTitle = "Pickwise";
    private const string MatchFoundTitle = "Pickwise - Match Found";
    private TrayIcon? _trayIcon;
    private MainWindow? _mainWindow;
    private AppPhase _lastPhase = AppPhase.WaitingForLeagueClient;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var log = new LocalDiagnosticLog();
            var viewModel = new MainViewModel(new KuncLcuClient(log), log);
            _mainWindow = new MainWindow
            {
                DataContext = viewModel,
            };
            desktop.MainWindow = _mainWindow;
            SetupTray(desktop, log);
            SetupReadyCheckAlert(viewModel);
            desktop.Exit += (_, _) => _trayIcon?.Dispose();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void SetupTray(IClassicDesktopStyleApplicationLifetime desktop, LocalDiagnosticLog log)
    {
        if (_mainWindow is null)
        {
            return;
        }

        try
        {
            var show = new NativeMenuItem("Show Pickwise");
            show.Click += (_, _) => ShowMainWindow();

            var exit = new NativeMenuItem("Exit");
            exit.Click += (_, _) =>
            {
                _trayIcon?.Dispose();
                desktop.Shutdown();
            };

            _trayIcon = new TrayIcon
            {
                Icon = new WindowIcon(System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "avalonia-logo.ico")),
                ToolTipText = NormalTitle,
                IsVisible = true,
                Menu = new NativeMenu
                {
                    Items =
                    {
                        show,
                        exit,
                    }
                }
            };
            _trayIcon.Clicked += (_, _) => ShowMainWindow();
        }
        catch (Exception exception)
        {
            log.Error("Tray setup failed", exception);
        }
    }

    private void SetupReadyCheckAlert(MainViewModel viewModel)
    {
        viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != nameof(MainViewModel.Phase))
            {
                return;
            }

            if (viewModel.Phase == AppPhase.ReadyCheck && _lastPhase != AppPhase.ReadyCheck)
            {
                SetAlertState(true);
                ShowMainWindow();
            }
            else if (viewModel.Phase != AppPhase.ReadyCheck && _lastPhase == AppPhase.ReadyCheck)
            {
                SetAlertState(false);
            }

            _lastPhase = viewModel.Phase;
        };
    }

    private void SetAlertState(bool matchFound)
    {
        var title = matchFound ? MatchFoundTitle : NormalTitle;
        if (_mainWindow is not null)
        {
            _mainWindow.Title = title;
        }

        if (_trayIcon is not null)
        {
            _trayIcon.ToolTipText = title;
        }
    }

    private void ShowMainWindow()
    {
        if (_mainWindow is null)
        {
            return;
        }

        _mainWindow.Show();
        _mainWindow.WindowState = WindowState.Normal;
        _mainWindow.Activate();
    }
}
