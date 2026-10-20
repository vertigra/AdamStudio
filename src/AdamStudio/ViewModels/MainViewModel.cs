using AdamStudio.EventArguments;
using AdamStudio.Model;
using AdamStudio.Services;
using AvalonDock.Core.Events;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Threading;

namespace AdamStudio.ViewModels;

/// <summary>
/// Главная ViewModel. Не зависит от конкретных типов Toolbox-ов —
/// все панели управляются через IToolboxService (регистрация, переключение, доступ).
///
/// MainViewModel зависит только от абстракций:
///   - IFileService        (диалоги открытия/сохранения файлов)
///   - IDialogService      (MessageBox)
///   - IWorkspaceService   (JSON-сохранение списка документов)
///   - IEditorTabViewModelFactory (создание документов через DI)
///   - IToolboxService     (управление панелями)
/// </summary>
public sealed partial class MainViewModel : ObservableObject, IWindowLifecycleAware
{
    #region Services

    private readonly IWorkspaceService mWorkspaceService;
    private readonly IEditorTabViewModelFactory mEditorFactory;
    private readonly IToolboxService mToolboxService;
    private readonly IDockingManagerProvider mDockingManagerProvider;
    private readonly ILayoutSerializerService mLayoutSerializerService;
    private readonly IThemeService mThemeService;
    private readonly IDocumentService mDocumentService;
    private readonly ILogService<MainViewModel> mLogService;

    #endregion

    #region Var
    private static readonly string AppDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AdamStudio");
    private static readonly string WorkspaceFilePath = Path.Combine(AppDataDir, "workspace.json");
    private static readonly string LayoutFilePath   = Path.Combine(AppDataDir, "layout.xml");
    private static readonly string SettingsFilePath = Path.Combine(AppDataDir, "settings.json");

    #endregion

    public MainViewModel(
        IWorkspaceService workspaceService,
        IEditorTabViewModelFactory factory,
        IToolboxService toolboxService,
        IDockingManagerProvider managerProvider,
        ILayoutSerializerService layoutSerializer,
        IThemeService themeService,
        IDocumentService dcumentService,
        ILogService<MainViewModel> logService,
        IDockEventBinder dockEventBinder)
    {
        mWorkspaceService = workspaceService;
        mEditorFactory = factory;
        mToolboxService = toolboxService;
        mDockingManagerProvider = managerProvider;
        mLayoutSerializerService = layoutSerializer;
        mThemeService = themeService;
        mDocumentService = dcumentService;
        mLogService = logService;

        // Регистрируем стартовые панели
        mToolboxService.Register<PropertiesToolbox>();
        mToolboxService.Register<OutputToolbox>();
        mToolboxService.Register<MainToolbox>();

        mLogService.LogInformation("Приложение запущено.");

        //CreateWelcomeDocument();
        LoadWorkspace();

        mDocumentService.PropertyChanged += OnDocumentServicePropertyChanged;
        //dockEventBinder.AnchorableHidden += DockEventBinder_AnchorableHidden;
    }

    private void DockEventBinder_AnchorableHidden(object? sender, AnchorableEventArgs e)
    {
        if (e.Anchorable.Content is ToolViewModelBase tool)
        {
            var s = 5;
            //tool.IsHidden = true;
        }
    }


    public ObservableCollection<EditorTabViewModel> Documents => mDocumentService.Documents;
    public EditorTabViewModel? ActiveDocument => mDocumentService.ActiveDocument;

    private void OnDocumentServicePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IDocumentService.ActiveDocument))
        {
            OnPropertyChanged(nameof(ActiveDocument));

            SaveFileCommand.NotifyCanExecuteChanged();
            CloseDocumentCommand.NotifyCanExecuteChanged();
        }
    }

    /// <summary>Панели-инструменты — делегируется в IToolboxService.</summary>
    public ObservableCollection<ToolViewModelBase> Anchorables => mToolboxService.Anchorables;

    /// <summary>Список доступных тем для меню «Вид → Темы».</summary>
    public IReadOnlyList<ThemeInfo> AvailableThemes => mThemeService.AvailableThemes;

    /// <summary>Имя текущей темы</summary>
    [ObservableProperty]
    public partial string? CurrentThemeName { get; set; }

    partial void OnCurrentThemeNameChanged(string? value)
    {
        if (!string.IsNullOrEmpty(value))
            mThemeService.ApplyTheme(value);
    }

    [RelayCommand]
    private void OpenNewDocument() => mDocumentService.CreateNew();

    [RelayCommand]
    private void OpenFile() => mDocumentService.OpenFile();

    [RelayCommand(CanExecute = nameof(CanSaveOrClose))]
    private void SaveFile()
    {
        if (ActiveDocument is { } doc) 
            mDocumentService.Save(doc);
    }

    [RelayCommand(CanExecute = nameof(CanSaveOrClose))]
    private void CloseDocument()
    {
        if (ActiveDocument is { } doc) 
            mDocumentService.Close(doc);
    }

    private bool CanSaveOrClose() => ActiveDocument is not null;

    [RelayCommand]
    private void ToggleProperties() => mToolboxService.Toggle<PropertiesToolbox>();

    [RelayCommand]
    private void ToggleOutput() => mToolboxService.Toggle<OutputToolbox>();

    [RelayCommand]
    private void ToggleMainToolbox() => mToolboxService.Toggle<MainToolbox>();

    [RelayCommand]
    private void Exit() => Application.Current.Shutdown();

    private void RestoreLayout()
    {
        var manager = mDockingManagerProvider.Manager;
        
        if (manager is null)
        {
            mLogService.LogWarning("DockingManager не загружен — восстановление невозможно.");
            return;
        }

        if (!File.Exists(LayoutFilePath))
        {
            mLogService.LogWarning($"Файл layout не найден — расположение по умолчанию: {LayoutFilePath}");
            return;
        }

        if (mLayoutSerializerService.Deserialize(manager, LayoutFilePath))
            mLogService.LogInformation($"Layout восстановлен: {LayoutFilePath}");
        else
            mLogService.LogWarning("Не удалось восстановить layout.");
    }

    private void LoadWorkspace()
    {
        WorkspaceState state = mWorkspaceService.Load(WorkspaceFilePath);

        if (state.Documents.Count == 0)
            return;

        foreach (var d in state.Documents)
        {
            EditorTabViewModel doc = mEditorFactory.CreateNew(d.Title, d.Content);
            doc.ContentId = d.ContentId;
            doc.FilePath = d.FilePath ?? string.Empty;
            doc.IsModified = d.IsDirty;

            Documents.Add(doc);
        }

        mDocumentService.NewDocCounter = state.NewDocumentCounter;
        mLogService.LogInformation($"Восстановлено документов: {state.Documents.Count}");
    }


    [RelayCommand]
    private void SetTheme(string? themeName)
    {
        if (string.IsNullOrEmpty(themeName)) 
            return;

        CurrentThemeName = themeName;
        mLogService.LogInformation($"Тема изменена: {themeName}");
    }

    // --- Сериализация рабочего состояния ---

    /// <summary>Вызывается из App.OnExit — сохраняет список открытых документов.</summary>
    public void OnWindowLoaded()
    {
        // Восстановление layout откладываем до следующего цикла Dispatcher-а:
        // в момент Loaded AvalonDock ещё не создал LayoutItem-ы для всех VM.
        Application.Current?.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () =>
            {
                try { RestoreLayout(); }
                catch (Exception ex) 
                {
                    mLogService.LogError("Ошибка восстановления layout при загрузке", ex);
                }
            });

        // Восстанавливаем настройки (тему) — менеджер уже зарегистрирован
        LoadSettings();
    }



    private void LoadSettings()
    {
        try
        {
            if (!File.Exists(SettingsFilePath)) 
                return;

            var json = File.ReadAllText(SettingsFilePath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json);

            if (settings?.ThemeName is { } theme)
            {
                CurrentThemeName = theme;
                mLogService.LogInformation($"Тема восстановлена: {theme}");
            }
        }
        catch { /* best-effort */ }
    }

    private void SaveSettings()
    {
        try
        {
            var settings = new AppSettings { ThemeName = CurrentThemeName };
            string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            Directory.CreateDirectory(AppDataDir);
            File.WriteAllText(SettingsFilePath, json);
        }
        catch { /* best-effort */ }
    }

    /// <summary>Вызывается из WindowLifecycleService при Closed окна.</summary>
    public void OnWindowClosed()
    {
        OnApplicationExiting();
    }

    public void OnApplicationExiting()
    {
        // Сохраняем layout (если менеджер ещё жив)
        try
        {
            var manager = mDockingManagerProvider.Manager;

            if (manager is null)
                return;
            
            mLayoutSerializerService.Serialize(manager, LayoutFilePath);
        }
        catch { /* best-effort */ }

        try { SaveWorkspace(); }
        catch { /* best-effort */ }

        try { SaveSettings(); } 
        catch { }
    }

    /*private void CreateWelcomeDocument()
    {
        var welcome = mEditorFactory.CreateNew(
            "Добро пожаловать",
            "AvalonDock 5 + .NET 10\r\nMVVM + Dependency Injection\r\n\r\n"
                + "Меню «Файл» → «Новый документ» или «Открыть файл...»\r\n"
                + "Переключение панелей: меню «Вид» или заголовки панелей");
        welcome.ContentId = "welcome";
        Documents.Add(welcome);*/
    //}

    private void SaveWorkspace()
    {
        var state = new WorkspaceState 
        { 
            NewDocumentCounter = mDocumentService.NewDocCounter
        };

        foreach (var doc in Documents)
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
}
