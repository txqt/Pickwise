using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using Pickwise.Models;

namespace Pickwise.ViewModels;

public partial class ChampionTileViewModel(Champion champion) : ViewModelBase
{
    public Champion Champion { get; } = champion;
    public string Name => Champion.Name;

    [ObservableProperty]
    private Bitmap? _icon;

    [ObservableProperty]
    private bool _isFavorite;

    [ObservableProperty]
    private bool _isQuickBan;

    public bool HasIcon => Icon is not null;
    public bool HasNoIcon => Icon is null;

    partial void OnIconChanged(Bitmap? value)
    {
        OnPropertyChanged(nameof(HasIcon));
        OnPropertyChanged(nameof(HasNoIcon));
    }
}
