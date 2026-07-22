using CommunityToolkit.Mvvm.ComponentModel;

namespace Pickwise.ViewModels;

public partial class ChampionRoleOptionViewModel(string name, string iconPath) : ViewModelBase
{
    public string Name { get; } = name;
    public string IconPath { get; } = iconPath;

    [ObservableProperty]
    private bool _isSelected;
}
