using AdamStudio.EventArguments;
using AdamStudio.Model;
using AdamStudio.Services;
using AvalonDock.Core.Events;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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

    private readonly IDialogService mDialogService;
    private readonly IWorkspaceService mWorkspaceService;
    private readonly IEditorTabViewModelFactory mEditorFactory;
    private readonly IToolboxService mToolboxService;
    private readonly IDockingManagerProvider mDockingManagerProvider;
    private readonly ILayoutSerializerService mLayoutSerializerService;
    private readonly IThemeService mThemeService;
    private readonly IDocumentService mDocumentService;

    #endregion

    #region Var
    private static readonly string AppDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AdamStudio");
    private static readonly string WorkspaceFilePath = Path.Combine(AppDataDir, "workspace.json");
    private static readonly string LayoutFilePath   = Path.Combine(AppDataDir, "layout.xml");
    private static readonly string SettingsFilePath = Path.Combine(AppDataDir, "settings.json");

    #endregion

    public MainViewModel(
        IDialogService dialog,
        IWorkspaceService workspaceService,
        IEditorTabViewModelFactory factory,
        IToolboxService toolboxService,
        IDockingManagerProvider managerProvider,
        ILayoutSerializerService layoutSerializer,
        IThemeService themeService,
        IDocumentService dcumentService)
    {
        mDialogService = dialog;
        mWorkspaceService = workspaceService;
        mEditorFactory = factory;
        mToolboxService = toolboxService;
        mDockingManagerProvider = managerProvider;
        mLayoutSerializerService = layoutSerializer;
        mThemeService = themeService;
        mDocumentService = dcumentService;

        // Регистрируем стартовые панели
        mToolboxService.Register<PropertiesToolbox>();
        mToolboxService.Register<OutputToolbox>();
        mToolboxService.Register<MainToolbox>();

        mToolboxService.Get<OutputToolbox>()?.AppendLine("Приложение запущено.");

        //CreateWelcomeDocument();
        LoadWorkspace();

        mDocumentService.PropertyChanged += OnDocumentServicePropertyChanged;
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

    /// <summary>Доступ к панели «Вывод» (для логирования из code-behind).</summary>
    public OutputToolbox? Output => mToolboxService.Get<OutputToolbox>();

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

    [RelayCommand]
    private void ToggleProperties() => mToolboxService.Toggle<PropertiesToolbox>();

    [RelayCommand]
    private void ToggleOutput() => mToolboxService.Toggle<OutputToolbox>();

    [RelayCommand]
    private void ToggleMainToolbox() => mToolboxService.Toggle<MainToolbox>();

    [RelayCommand]
    private void Exit() => Application.Current.Shutdown();

    [RelayCommand]
    private void SaveLayout()
    {
        var manager = mDockingManagerProvider.Manager;
        if (manager is null)
        {
            Output?.AppendLine("DockingManager не загружен — сохранение невозможно.");
            return;
        }

        try
        {
            mLayoutSerializerService.Serialize(manager, LayoutFilePath);
            Output?.AppendLine($"Layout сохранён: {LayoutFilePath}");
        }
        catch (Exception ex)
        {
            mDialogService.ShowInformation($"Ошибка сохранения layout: {ex.Message}", "Ошибка");
        }
    }

    [RelayCommand]
    private void LoadLayout()
    {
        var manager = mDockingManagerProvider.Manager;
        if (manager is null)
        {
            Output?.AppendLine("DockingManager не загружен — восстановление невозможно.");
            return;
        }

        try
        {
            if (mLayoutSerializerService.Deserialize(manager, LayoutFilePath))
                Output?.AppendLine($"Layout восстановлен: {LayoutFilePath}");
            else
                Output?.AppendLine("Файл layout не найден — расположение по умолчанию.");
        }
        catch (Exception ex)
        {
            mDialogService.ShowInformation($"Ошибка загрузки layout: {ex.Message}", "Ошибка");
        }
    }

    private bool CanSaveOrClose() => ActiveDocument is not null;

    [RelayCommand]
    private void SetTheme(string? themeName)
    {
        if (string.IsNullOrEmpty(themeName)) 
            return;

        CurrentThemeName = themeName;
        Output?.AppendLine($"Тема изменена: {themeName}");
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
                catch (Exception ex) { Output?.AppendLine($"Ошибка восстановления layout: {ex.Message}"); }
            });

        // Восстанавливаем настройки (тему) — менеджер уже зарегистрирован
        LoadSettings();
    }

    private void RestoreLayout()
    {
        var manager = mDockingManagerProvider.Manager;
        if (manager is null) return;

        if (!File.Exists(LayoutFilePath))
        {
            Output?.AppendLine("Файл layout не найден — расположение по умолчанию.");
            return;
        }

        if (mLayoutSerializerService.Deserialize(manager, LayoutFilePath))
            Output?.AppendLine($"Layout восстановлен: {LayoutFilePath}");
        else
            Output?.AppendLine("Не удалось восстановить layout.");
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
                Output?.AppendLine($"Тема восстановлена: {theme}");
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

    private void CreateWelcomeDocument()
    {
        var welcome = mEditorFactory.CreateNew(
            "Добро пожаловать",
            "AvalonDock 5 + .NET 10\r\nMVVM + Dependency Injection\r\n\r\n"
                + "Меню «Файл» → «Новый документ» или «Открыть файл...»\r\n"
                + "Переключение панелей: меню «Вид» или заголовки панелей");
        welcome.ContentId = "welcome";
        Documents.Add(welcome);
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
        Output?.AppendLine($"Восстановлено документов: {state.Documents.Count}");
    }

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
