using AdamStudio.Extensions;
using AdamStudio.Services;
using AdamStudio.ViewModels;
using AdamStudio.Views;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.ObjectModel;
using System.Windows;

namespace AdamStudio;

public partial class App : Application
{
    private readonly IHost mHost;
    private readonly ObservableCollection<string> mUiLogLines = [];

    /// <summary>
    /// Переключатель уровня логирования для UI-панели.
    /// Доступен глобально — можно менять в runtime без перезапуска.
    /// </summary>
    public static LoggingLevelSwitch OutputPanelLevelSwitch { get; } = new(LogEventLevel.Information);

    public App()
    {

        // 1. Читаем конфигурацию из appsettings.json
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        // Читаем уровень для UI из конфига (с fallback на Information)
        var uiLevel = configuration.GetValue("UI:OutputPanel:MinimumLevel", LogEventLevel.Information);
        OutputPanelLevelSwitch.MinimumLevel = uiLevel;

        // 2. Создаём Serilog logger из конфигурации
        //    ObservableSink добавляем программно — он ссылается на _uiLogLines
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .WriteTo.ObservableSink(mUiLogLines, levelSwitch: OutputPanelLevelSwitch)
            .CreateLogger();

        mHost = Host.CreateDefaultBuilder()
            .ConfigureServices((_, services) =>
            {
                services.AddSingleton(sp => sp);

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
        var vm = mHost.Services.GetService<MainViewModel>();
        vm?.OnApplicationExiting();

        await mHost.StopAsync();
        mHost.Dispose();
        Log.CloseAndFlush();
        base.OnExit(e);
    }
}
