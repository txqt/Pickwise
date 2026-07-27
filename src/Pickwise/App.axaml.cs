using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Pickwise.Models;
using Pickwise.Services;
using Pickwise.ViewModels;
using Pickwise.Views;
using System.Runtime.InteropServices;

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
            SetupReadyCheckAlert(viewModel, log);
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

    private void SetupReadyCheckAlert(MainViewModel viewModel, LocalDiagnosticLog log)
    {
        viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != nameof(MainViewModel.Phase))
            {
                return;
            }

            if (ShouldShowReadyCheckAlert(_lastPhase, viewModel.Phase))
            {
                SetAlertState(true);
                PlayReadyCheckSound(log);
                ShowMainWindow();
            }
            else if (ShouldClearReadyCheckAlert(_lastPhase, viewModel.Phase))
            {
                SetAlertState(false);
            }

            _lastPhase = viewModel.Phase;
        };
    }

    public static bool ShouldShowReadyCheckAlert(AppPhase lastPhase, AppPhase currentPhase) =>
        currentPhase == AppPhase.ReadyCheck && lastPhase != AppPhase.ReadyCheck;

    public static bool ShouldClearReadyCheckAlert(AppPhase lastPhase, AppPhase currentPhase) =>
        currentPhase != AppPhase.ReadyCheck && lastPhase == AppPhase.ReadyCheck;

    private static void PlayReadyCheckSound(LocalDiagnosticLog log)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        try
        {
            _ = MessageBeep(0x00000030);
        }
        catch (Exception exception)
        {
            log.Error("Ready Check sound failed", exception);
        }
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

    [DllImport("user32.dll")]
    private static extern bool MessageBeep(uint type);
}
