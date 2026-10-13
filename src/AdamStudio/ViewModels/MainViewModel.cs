using AdamStudio.EventArguments;
using AdamStudio.Model;
using AdamStudio.Services;
using AvalonDock.Core.Events;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
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
    private readonly IFileService mFileService;
    private readonly IDialogService mDialogService;
    private readonly IWorkspaceService mWorkspaceService;
    private readonly IEditorTabViewModelFactory mEditorTabFactory;
    private readonly IToolboxService mToolboxService;
    private readonly IDockingManagerProvider mDockingManagerProvider;
    private readonly IDockEventBinder mDockEvent;
    private readonly ILayoutSerializerService mLayoutSerializerService;
    private readonly IThemeService mThemeService;
    //private int mNewDocCounter;
    private bool mIsRemovingFromCode;
    private static readonly string AppDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AdamStudio");
    private static readonly string WorkspaceFilePath = Path.Combine(AppDataDir, "workspace.json");
    private static readonly string LayoutFilePath   = Path.Combine(AppDataDir, "layout.xml");
    private static readonly string SettingsFilePath = Path.Combine(AppDataDir, "settings.json");

    public MainViewModel(
        IFileService fileService,
        IDialogService dialog,
        IWorkspaceService workspaceService,
        IEditorTabViewModelFactory factory,
        IToolboxService toolboxService,
        IDockingManagerProvider managerProvider,
        IDockEventBinder dockBinder,
        ILayoutSerializerService layoutSerializer,
        IThemeService themeService)
    {
        mFileService = fileService;
        mDialogService = dialog;
        mWorkspaceService = workspaceService;
        mEditorTabFactory = factory;
        mToolboxService = toolboxService;
        mDockingManagerProvider = managerProvider;
        mDockEvent = dockBinder;
        mLayoutSerializerService = layoutSerializer;
        mThemeService = themeService;

        // Регистрируем стартовые панели
        mToolboxService.Register<PropertiesToolbox>();
        mToolboxService.Register<OutputToolbox>();
        mToolboxService.Register<MainToolbox>();

        mToolboxService.Get<OutputToolbox>()?.AppendLine("Приложение запущено.");

        // Подписываемся на события DockingManager через binder.
        mDockEvent.ActiveContentChanged += OnActiveContentChanged;
        mDockEvent.DocumentClosing += OnDocumentClosing;
        mDockEvent.DocumentClosed += OnDocumentClosed;
        mDockEvent.AnchorableClosed += OnAnchorableClosed;
        mDockEvent.Activate();

        //CreateWelcomeDocument();
        LoadWorkspace();
    }

    /// <summary>Открытые документы (вкладки в центре).</summary>
    public ObservableCollection<EditorTabViewModel> Documents { get; } = [];

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

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveFileCommand))]
    [NotifyCanExecuteChangedFor(nameof(CloseDocumentCommand))]
    public partial EditorTabViewModel? ActiveDocument { get; set; }

    // --- Команды ---

    [RelayCommand]
    private void OpenNewDocument()
    {
        //mNewDocCounter++;

        EditorTabViewModel doc = mEditorTabFactory.CreateNew($"Документ", $"Содержимое документа.\r\nСоздан: {DateTime.Now:G}");
        Documents.Add(doc);
        ActiveDocument = doc;

        Output?.AppendLine($"Открыт документ: {doc.Title}");
    }

    [RelayCommand]
    private void OpenFile()
    {
        var path = mFileService.ShowOpenDialog();
        if (path is null) return;

        try
        {
            // Если файл уже открыт — активируем существующую вкладку
            EditorTabViewModel? existing = Documents.FirstOrDefault(d => d.FilePath == path);
            if (existing != null)
            {
                existing.IsActive = true;
                ActiveDocument = existing;                
                Output?.AppendLine($"Активирован существующий: {path}");
                return;
            }

            EditorTabViewModel tab = mEditorTabFactory.CreateFromFile(path);
            Documents.Add(tab);
            ActiveDocument = tab;
            Output?.AppendLine($"Открыт файл: {path}");
        }
        catch (Exception ex)
        {
            mDialogService.ShowInformation($"Не удалось открыть файл: {ex.Message}", "Ошибка");
        }
    }

    [RelayCommand(CanExecute = nameof(CanSaveOrClose))]
    private void SaveFile()
    {
        if (ActiveDocument is not { } doc) 
            return;

        try
        {
            if (doc.SaveToFile(suggested => mFileService.ShowSaveDialog(suggested), mFileService.WriteAllText))
            {
                Output?.AppendLine($"Сохранён файл: {doc.FilePath}");
            }
        }
        catch (Exception ex)
        {
            mDialogService.ShowInformation($"Не удалось сохранить файл: {ex.Message}", "Ошибка");
        }
    }

    [RelayCommand(CanExecute = nameof(CanSaveOrClose))]
    private void CloseDocument()
    {
        if (ActiveDocument is not { } doc) 
            return;

        if (!ConfirmClose(doc)) 
            return;

        Documents.Remove(doc);
        Output?.AppendLine($"Закрыт документ: {doc.Title}");
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

    // --- Подтверждение закрытия ---

    private void OnActiveContentChanged(object? sender, ActiveContentChangedEventArgs e)
    {
        if (e.NewContent is EditorTabViewModel doc)
            ActiveDocument = doc;

        else if (e.NewContent is null)
            ActiveDocument = null;
    }

    private void OnDocumentClosing(object? sender, DocumentCancelEventArgs e)
    {
        if (e.Document.Content is not EditorTabViewModel doc) 
            return;

        if (!ConfirmClose(doc))
            e.Cancel = true;
    }

    private void OnDocumentClosed(object? sender, DocumentEventArgs e)
    {
        if (mIsRemovingFromCode)
            return;

        if (e.Document.Content is EditorTabViewModel doc)
        {
            try
            {
                mIsRemovingFromCode = true;

                Documents.Remove(doc);
                Output?.AppendLine($"Документ закрыт пользователем: {doc.Title}");

            }
            finally
            {
                mIsRemovingFromCode = false;
            }
        }
    }

    /// <summary>
    /// Вызывается из MainWindow.xaml.cs по событию DockingManager.DocumentClosing.
    /// Возвращает false, чтобы отменить закрытие.
    /// </summary>
    public bool ConfirmClose(EditorTabViewModel doc)
    {
        if (!doc.IsModified)
            return true;

        var result = mDialogService.ShowQuestion(
            $"Сохранить изменения в документе «{doc.Title}»?",
            "Несохранённые изменения");

        Output?.AppendLine($"Закрытие «{doc.Title}»: выбор = {result}");

        if (result == MessageBoxResult.Yes)
        {
            return doc.SaveToFile(suggested => mFileService.ShowSaveDialog(suggested), mFileService.WriteAllText);
        }

        return result != MessageBoxResult.Cancel;
    }

    private void OnAnchorableClosed(object? sender, AnchorableEventArgs e)
    {
        if (e.Anchorable.Content is ToolViewModelBase tool)
        {
            tool.IsOpen = false;
            Output?.AppendLine($"Панель скрыта: {tool.Title}");
        }
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
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
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
        var welcome = mEditorTabFactory.CreateNew(
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
            EditorTabViewModel doc = mEditorTabFactory.CreateNew(d.Title, d.Content);
            doc.ContentId = d.ContentId;
            doc.FilePath = d.FilePath ?? string.Empty;
            doc.IsModified = d.IsDirty;

            Documents.Add(doc);
        }

        mEditorTabFactory.NewDocCounter = state.NewDocumentCounter;
        Output?.AppendLine($"Восстановлено документов: {state.Documents.Count}");
    }

    private void SaveWorkspace()
    {
        var state = new WorkspaceState 
        { 
            NewDocumentCounter = mEditorTabFactory.NewDocCounter
        };

        foreach (var doc in Documents)
        {
            //если документ не сохранен, не храним его состояние
            //if (string.IsNullOrEmpty(doc.FilePath))
            //    return;

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
