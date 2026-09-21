using System.Windows;
using YtdlpDesktop.ViewModels;

namespace YtdlpDesktop.Views;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
