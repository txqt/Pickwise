using Avalonia.Controls;
using Avalonia.Input;
using Pickwise.ViewModels;

namespace Pickwise.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void ChampionsList_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel
            && ChampionsList.SelectedItem is ChampionTileViewModel tile
            && viewModel.PickChampionTileCommand.CanExecute(tile))
        {
            viewModel.PickChampionTileCommand.Execute(tile);
        }
    }
}
