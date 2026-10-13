using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using AdamStudio.Services;
using AdamStudio.ViewModels;
using AdamStudio.Views;
using System;

namespace AdamStudio;

public partial class App : Application
{
    private readonly IHost mHost;

    public App()
    {
        mHost = Host.CreateDefaultBuilder()
            .ConfigureServices((_, services) =>
            {
                // --- Сервисы приложения ---
                services.AddSingleton(sp => sp);
                services.AddSingleton<IDialogService, DialogService>();
                services.AddSingleton<IFileService, FileService>();
                services.AddSingleton<IWorkspaceService, WorkspaceService>();

                // Holder для DockingManager
                services.AddSingleton<IDockingManagerProvider, DockingManagerProvider>();

                // Подписка на события DockingManager 
                services.AddSingleton<IDockEventBinder, DockEventBinder>();

                // Жизненный цикл окна (Loaded/Closed) 
                services.AddSingleton<IWindowLifecycleService, WindowLifecycleService>();

                // Сериализация layout (положение панелей, ширины, floating-окна)
                services.AddSingleton<ILayoutSerializerService, LayoutSerializerService>();

                // Управление темами оформления
                services.AddSingleton<IThemeService, ThemeService>();

                // Сервис управления панелями (Toolbox)
                services.AddSingleton<IToolboxService, ToolboxService>();

                // Фабрика для создания документов через DI (вместо new EditorTabViewModel())
                services.AddSingleton<IEditorTabViewModelFactory, EditorTabViewModelFactory>();

                // --- Документы: каждый раз новый экземпляр (transient) ---
                services.AddTransient<EditorTabViewModel>();

                // --- Панели (один экземпляр на всё приложение) ---
                services.AddSingleton<PropertiesToolbox>();
                services.AddSingleton<OutputToolbox>();
                services.AddSingleton<MainToolbox>();

                // --- Главная ViewModel ---
                services.AddSingleton<MainViewModel>();

                // --- Главное окно ---
                services.AddSingleton<MainWindow>();
            })
            .Build();
    }

    public static new App Current => (App)Application.Current;
    public IServiceProvider Services => mHost.Services;

    protected override async void OnStartup(StartupEventArgs e)
    {
        await mHost.StartAsync();

        var mainWindow = mHost.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();

        base.OnStartup(e);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        var vm = mHost.Services.GetService<MainViewModel>();
        vm?.OnApplicationExiting();

        await mHost.StopAsync();
        mHost.Dispose();
        base.OnExit(e);
    }
}
