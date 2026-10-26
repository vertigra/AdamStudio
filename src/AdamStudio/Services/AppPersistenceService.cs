using AdamStudio.Model;
using Serilog;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace AdamStudio.Services;

/// <summary>
/// Единый сервис персистентности приложения. Инкапсулирует:
/// - сохранение/восстановление layout (layout.xml)
/// - сохранение/восстановление workspace (workspace.json — список документов
/// - debounced автосохранение layout при изменениях
///
/// MainViewModel делегирует сюда всё — никаких путей к файлам и сериализации в VM.
/// </summary>
public interface IAppPersistenceService
{
    /// <summary>
    /// Сообщает подписчикам что необходимо восстановить настройки
    /// </summary>
    event EventHandler? RestoreHappened;

    /// <summary>Восстановить всё состояние при старте приложения включая состояние, вызывает событие RestoreHappened</summary>
    void Restore();

    /// <summary>Сохранить всё состояние (выход, явная команда).</summary>
    void Save();

    /// <summary>Запланировать отложенное сохранение layout (debounce 500мс).</summary>
    void ScheduleLayoutSave();
}

public sealed class AppPersistenceService : IAppPersistenceService
{
    public event EventHandler? RestoreHappened;

    private readonly IDockingManagerProvider mDockingManager;
    private readonly ILayoutSerializerService mLayoutSerializer;
    private readonly IWorkspaceService mWorkspaceService;
    private readonly IEditorTabViewModelFactory mEditorTabFactory;
    private readonly IDocumentService mDocumentService;
    private readonly ISettingsService mSettingsService;
    private readonly ILogService<AppPersistenceService> mLoggerService;

    private static readonly string AppDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AdamStudio");
    private static readonly string WorkspaceFilePath = Path.Combine(AppDataDir, "workspace.json");
    private static readonly string LayoutFilePath = Path.Combine(AppDataDir, "layout.xml");

    private int mLayoutChangeCount;
    private const int cSaveLayoutDelayMs = 500;

    public AppPersistenceService(
        IDockingManagerProvider managerProvider,
        ILayoutSerializerService layoutSerializer,
        IWorkspaceService workspaceService,
        IEditorTabViewModelFactory factory,
        IDocumentService documentService,
        ISettingsService settingsService,
        IDockEventBinder dockEventBinder,
        ILogService<AppPersistenceService> loggerService)
    {
        mDockingManager = managerProvider;
        mLayoutSerializer = layoutSerializer;
        mWorkspaceService = workspaceService;
        mEditorTabFactory = factory;
        mDocumentService = documentService;
        mSettingsService = settingsService;
        mLoggerService = loggerService;

        mSettingsService.Current.PropertyChanged += SettingsPropertyChanged;
        dockEventBinder.LayoutChanged += OnLayoutChanged;
    }

    private void OnLayoutChanged(object? sender, EventArgs e)
    {
        ScheduleLayoutSave();
    }

    public void Save()
    {
        try 
        { 
            SaveLayout(); 
        }
        catch (Exception ex) 
        { 
            mLoggerService.LogError("SaveLayout failed", ex); 
        }

        try 
        { 
            SaveWorkspace(); 
        }
        catch (Exception ex) 
        { 
            mLoggerService.LogError("SaveWorkspace failed", ex); 
        }
    }


    public void Restore()
    {
        NotifyRestoreCompleted();
        RestoreWorkspace();
        RestoreLayout();
    }

    private void RestoreWorkspace()
    {
        var state = mWorkspaceService.Load(WorkspaceFilePath);
        if (state.Documents.Count == 0) 
            return;

        foreach (var d in state.Documents)
        {
            var doc = mEditorTabFactory.CreateNew(d.Title, d.Content);
            doc.ContentId = d.ContentId;
            doc.FilePath = d.FilePath ?? string.Empty;
            doc.IsModified = d.IsDirty;
            mDocumentService.Documents.Add(doc);
        }
        mDocumentService.NewDocCounter = state.NewDocumentCounter;
        mLoggerService.LogInformation($"Восстановлено документов: {state.Documents.Count}");
    }

    private void RestoreLayout()
    {
        var manager = mDockingManager.Manager;
        if (manager is null)
        {
            mLoggerService.LogWarning("DockingManager не загружен — восстановление layout невозможно.");
            return;
        }

        if (!File.Exists(LayoutFilePath))
        {
            mLoggerService.LogWarning($"Файл layout не найден: {LayoutFilePath}");
            return;
        }

        // Восстановление откладываем — AvalonDock ещё не создал LayoutItem-ы
        Application.Current?.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () =>
        {
            try
            {
                if (mLayoutSerializer.Deserialize(manager, LayoutFilePath))
                    mLoggerService.LogInformation($"Layout восстановлен: {LayoutFilePath}");
                else
                    mLoggerService.LogWarning("Не удалось восстановить layout.");
            }
            catch (Exception ex)
            {
                mLoggerService.LogError("Ошибка восстановления layout", ex);
            }
        });
    }

    /// <summary>Debounced сохранение layout — вызывать при каждом LayoutChanged.</summary>
    public void ScheduleLayoutSave()
    {
        Interlocked.Increment(ref mLayoutChangeCount);
        Task.Run(async () =>
        {
            await Task.Delay(cSaveLayoutDelayMs);
            if (Interlocked.Decrement(ref mLayoutChangeCount) == 0)
            {
                try
                {
                    mLoggerService.LogDebug("Автосохранение layout");

                    // ВАЖНО: сериализация обращается к DockingManager.Layout (DependencyProperty),
                    // а значит должна выполняться в UI-потоке. Marshal через Dispatcher.
                    Application.Current?.Dispatcher.BeginInvoke(() =>
                    {
                        SaveLayout();
                    });
                }
                catch (Exception ex)
                {
                    mLoggerService.LogError("Автосохранение layout failed", ex);
                }
            }
        });
    }

    private void SaveLayout()
    {
        var manager = mDockingManager.Manager;
        
        if (manager is null) 
            return;

        mLayoutSerializer.Serialize(manager, LayoutFilePath);
    }

    private void SaveWorkspace()
    {
        var state = new WorkspaceState
        {
            NewDocumentCounter = mDocumentService.NewDocCounter
        };

        foreach (var doc in mDocumentService.Documents)
        {
            state.Documents.Add(new WorkspaceDocument
            {
                ContentId = doc.ContentId,
                Title = doc.DocumentName,
                FilePath = string.IsNullOrEmpty(doc.FilePath) ? null : doc.FilePath,
                Content = doc.Content,
                IsDirty = doc.IsModified
            });
        }

        mWorkspaceService.Save(WorkspaceFilePath, state);
    }

    /// <summary>
    /// Сообщет сервисам что необходимо восстановить настройки
    /// </summary>
    private void NotifyRestoreCompleted() => RestoreHappened?.Invoke(this, new EventArgs());

    /// <summary>
    /// Получает сигнал от сервиса настроек что настройки изменены.Исключает пересохранение при загрузке
    /// </summary>
    private void SettingsPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e) => mSettingsService.Save();
}