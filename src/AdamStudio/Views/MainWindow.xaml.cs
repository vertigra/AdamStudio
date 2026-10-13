using System.Windows;
using AdamStudio.Services;
using AdamStudio.ViewModels;

namespace AdamStudio.Views;

/// <summary>
/// Code-behind минимален: только установка DataContext из DI
/// и подключение сервиса жизненного цикла.
/// Вся логика — во ViewModel и сервисах.
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel, IWindowLifecycleService lifecycle)
    {
        DataContext = viewModel;
        InitializeComponent();
        lifecycle.Attach(this, dockManager);
    }
}
