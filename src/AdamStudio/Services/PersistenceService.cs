using AdamStudio.Core.AvalonDock.BaseViewModels;
using AdamStudio.Core.AvalonDock.Model;
using AdamStudio.Core.AvalonDock.Services;
using AdamStudio.Core.Services;
using AdamStudio.Factories;
using AdamStudio.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace AdamStudio.Services;

public sealed class PersistenceService : IPersistenceService
{
    public event EventHandler? RestoreHappened;

    private readonly IDockingManagerProvider mDockingManager;
    private readonly ILayoutSerializerService mLayoutSerializer;
    private readonly IWorkspaceService mWorkspaceService;
    private readonly IEditorTabViewModelFactory mEditorFactory;
    private readonly IDockWindowService mDockWindowService;
    private readonly ISettingsService mSettingsService;

    private readonly IServiceProvider mServiceProvider;
    private readonly ILogService<PersistenceService> mLoggerService;

    private static readonly string AppDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AdamStudio");
    private static readonly string WorkspaceFilePath = Path.Combine(AppDataDir, "workspace.json");
    private static readonly string LayoutFilePath = Path.Combine(AppDataDir, "layout.xml");

    private int mLayoutChangeCount;
    private const int cSaveLayoutDelayMs = 500;

    public PersistenceService(
        IDockingManagerProvider managerProvider,
        ILayoutSerializerService layoutSerializer,
        IWorkspaceService workspaceService,
        IEditorTabViewModelFactory editorFactory,
        IDockEventBinder dockEventBinder,
        ISettingsService settingsService,
        IDockWindowService dockWindowService,
        IServiceProvider serviceProvider,
        ILogService<PersistenceService> loggerService)
    {
        mDockingManager = managerProvider;
        mLayoutSerializer = layoutSerializer;
        mWorkspaceService = workspaceService;
        mEditorFactory = editorFactory;
        mSettingsService = settingsService;
        mDockWindowService = dockWindowService;
        mServiceProvider = serviceProvider;
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
        WorkspaceState state = mWorkspaceService.Load(WorkspaceFilePath);

        if (state.Documents.Count == 0)
        {
            mLoggerService.LogInformation($"Нет документов для восстановления");
            return;
        }
            

        foreach (WorkspaceDocument d in state.Documents)
        {
            DocumentViewModelBase? doc = null;

            // Восстанавливаем в зависимости от типа
            if (d.DocumentTypeName == typeof(EditorTabViewModel).FullName)
            {
                doc = mEditorFactory.LoadDocumentState(d);
            }
            else if (d.DocumentTypeName == typeof(SettingsViewModel).FullName)
            {
                // Для настроек берём синглтон из DI
                doc = mServiceProvider.GetRequiredService<SettingsViewModel>();
                // восстанавливаем динамичные настройки, остальные статичны и смысла их восстанавливать нет
                doc.IsActive = d.IsActive;
                doc.IsModified = d.IsModified;
            }

            if (doc != null)
                mDockWindowService.OpenDocument(doc);
        }

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
                {
                    mLoggerService.LogInformation($"Layout восстановлен: {LayoutFilePath}");
                    
                    if (manager.ActiveContent is DocumentViewModelBase activeDoc)
                    {
                        mDockWindowService.ActivateDocument(activeDoc);

                        mLoggerService.LogInformation($"Активный документ восстановлен: {activeDoc.Title}");

                        return;
                    }

                    mLoggerService.LogInformation($"Активных документов для восстановления не обнаружено");

                    return;
                }
                
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
        var state = new WorkspaceState();

        foreach (var doc in mDockWindowService.Documents)
        {
            var workspaceDocument = new WorkspaceDocument
            {
                ContentId = doc.ContentId,
                Title = doc.Title,
                IsActive = doc.IsActive,
                IsModified = doc.IsModified,
                DocumentTypeName = doc.GetType().FullName ?? "Undefined" // Сохраняем тип, чтобы знать, что создавать при загрузке

            };

            // Если это текстовый редактор — сохраняем текст и путь
            if (doc is EditorTabViewModel editor)
            {
                workspaceDocument.FilePath = string.IsNullOrEmpty(editor.FilePath) ? null : editor.FilePath;
                workspaceDocument.Content = editor.Content;
            }

            state.Documents.Add(workspaceDocument);
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
    private void SettingsPropertyChanged(object? sender, PropertyChangedEventArgs e) => mSettingsService.Save();
}