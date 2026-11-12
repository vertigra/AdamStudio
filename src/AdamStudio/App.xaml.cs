using AdamStudio.Core.AvalonDock.Services;
using AdamStudio.Core.Services;
using AdamStudio.Core.Services.AvalonDock;
using AdamStudio.Extensions;
using AdamStudio.Factories;
using AdamStudio.Services;
using AdamStudio.ViewModels;
using AdamStudio.Views;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
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
        /*Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .MinimumLevel.Verbose()  // Пропускаем ВСЕ сообщения в пайплайн
            .WriteTo.Async(a => a.File("logs/app.log",
                                rollingInterval: RollingInterval.Day,
                                retainedFileCountLimit: 14,
                                levelSwitch: settingsService.GlobalLevelSwitch,  // ← File sink фильтруется через GlobalLevelSwitch
                                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}"))

            .WriteTo.ObservableSink(mUiLogLines, levelSwitch: settingsService.OutputPanelSwitch)
            .CreateLogger();*/

        var settings = settingsService.Current;

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Async(a => a.File(
                settings.LogFilePath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: settings.RetainedFileCountLimit,
                levelSwitch: settingsService.GlobalLevelSwitch,
                outputTemplate: settings.FileLogTemplate
            ))
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
                services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(dispose: true));

                // Базовый логер (без контекста) — для тех, кому не нужен SourceContext
                services.AddSingleton<ILogService>(sp => new LogService(sp.GetRequiredService<ILogger>(), mUiLogLines));
   
                // Типизированный логер — добавляет SourceContext автоматически
                services.AddSingleton(typeof(ILogService<>), typeof(LogService<>));

                services.AddSingleton<IPersistenceService, PersistenceService>();
                // --- Сервисы приложения ---

                services.AddSingleton<IDialogService, DialogService>();
                services.AddSingleton<IFileService, FileService>();
                services.AddSingleton<IWorkspaceService, WorkspaceService>();

                // Слабосвязанные сообщения
                services.AddSingleton<IMessenger, WeakReferenceMessenger>();
                // Holder для DockingManager
                services.AddSingleton<IDockingManagerProvider, DockingManagerProvider>();

                // Подписка на события DockingManager 
                services.AddSingleton<IDockEventBinder, DockEventBinder>();

                // Жизненный цикл окна (Loaded/Closed) 
                services.AddSingleton<IWindowLifecycleService, WindowLifecycleService>();

                // Сериализация layout (положение панелей, ширины, floating-окна)
                services.AddSingleton<ILayoutSerializerService, LayoutSerializerService>();

                // фабрика тем, с типами AvalonDock
                services.AddSingleton<IThemeFactoryService, ThemeFactoryService>();
                // Управление темами оформления
                services.AddSingleton<IThemeService, ThemeService>();

                // Сервис управления панелями (Toolbox)
                services.AddSingleton<IToolboxService, ToolboxService>();

                // Фабрика для создания документов через DI (вместо new EditorTabViewModel())
                services.AddSingleton<IEditorTabViewModelFactory, EditorTabViewModelFactory>();

                // Сервис по управлению документами
                services.AddSingleton<IDockWindowService, DockWindowService>();

                // --- Документы: каждый раз новый экземпляр (transient) ---
                services.AddTransient<EditorTabViewModel>();

                // --- Документы: каждый раз один экземпляр (singleton) ---
                //services.AddSettingsDocument();
                //services.AddSingleton<SettingsViewModel>();

                services.AddSingleton<SettingsViewModel>();
                

                // --- Панели (один экземпляр на всё приложение) ---
                services.AddSingleton<PropertiesToolboxViewModel>();
                services.AddSingleton<OutputToolboxViewModel>();

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

        _ = mHost.Services.GetRequiredService<SettingsViewModel>();

        var mainWindow = mHost.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();

        base.OnStartup(e);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        await mHost.StopAsync();
        mHost.Dispose();
        Log.CloseAndFlush();
        base.OnExit(e);
    }
}
