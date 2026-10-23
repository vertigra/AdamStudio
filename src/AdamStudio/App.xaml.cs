using AdamStudio.Extensions;
using AdamStudio.Services;
using AdamStudio.ViewModels;
using AdamStudio.Views;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System.Collections.ObjectModel;
using System.Windows;

namespace AdamStudio;

public partial class App : Application
{
    private readonly IHost mHost;
    private readonly ObservableCollection<string> mUiLogLines = [];
    public App()
    {
        var settingsService = new SettingsService();

        // 1. Читаем конфигурацию из appsettings.json
        var configuration = new ConfigurationBuilder()
            .SetBasePath(settingsService.SettingsDirectory)
            .AddJsonFile(settingsService.SettingsFileName, optional: false, reloadOnChange: true)
            .Build();

        // 2. Создаём Serilog logger из конфигурации
        //    ObservableSink добавляем программно — он ссылается на _uiLogLines
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .MinimumLevel.ControlledBy(settingsService.GlobalLevelSwitch)
            .WriteTo.ObservableSink(mUiLogLines, levelSwitch: settingsService.OutputPanelSwitch)
            .CreateLogger();

        mHost = Host.CreateDefaultBuilder()
            .ConfigureServices((_, services) =>
            {
                services.AddSingleton(sp => sp);

                services.AddSingleton<ISettingsService>(settingsService);
                services.AddSingleton<IConfiguration>(configuration);

                // Serilog.ILogger — глобальный логер
                services.AddSingleton(_ => Log.Logger);

                // Базовый логер (без контекста) — для тех, кому не нужен SourceContext
                services.AddSingleton<ILogService>(sp => new LogService(sp.GetRequiredService<ILogger>(), mUiLogLines));
   
                // Типизированный логер — добавляет SourceContext автоматически
                services.AddSingleton(typeof(ILogService<>), typeof(LogService<>));

                services.AddSingleton<IAppPersistenceService, AppPersistenceService>();
                // --- Сервисы приложения ---

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

                // Сервис по управлению документами
                services.AddSingleton<IDocumentService, DocumentService>();

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
            .UseSerilog()
            .Build();
    }

    //public static new App Current => (App)Application.Current;
    //public IServiceProvider Services => mHost.Services;

    protected override async void OnStartup(StartupEventArgs e)
    {
        await mHost.StartAsync();

        var mainWindow = mHost.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();

        base.OnStartup(e);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        //var vm = mHost.Services.GetService<MainViewModel>();
        //vm?.OnApplicationExiting();

        await mHost.StopAsync();
        mHost.Dispose();
        Log.CloseAndFlush();
        base.OnExit(e);
    }
}
