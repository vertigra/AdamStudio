using AdamStudio.Core.AvalonDock.BaseViewModels;
using AdamStudio.Core.AvalonDock.Model;
using AdamStudio.Core.AvalonDock.Services;
using AdamStudio.Core.Services;
using AdamStudio.Core.Services.AvalonDock;
using AdamStudio.Factories;
using AdamStudio.Model;
using AdamStudio.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;

namespace AdamStudio.ViewModels;

public sealed partial class MainViewModel : ObservableObject, IWindowLifecycleAware
{
    #region Services

    private readonly IToolboxService mToolboxService;
    private readonly IThemeService mThemeService;
    private readonly IDockWindowService mDockWindowService;
    private readonly IPersistenceService mPersistenceService;
    private readonly ILogService<MainViewModel> mLogService;
    private readonly IEditorTabViewModelFactory mEditorFactory;
    private readonly IFileService mFileService;

    private readonly IDialogService mDialogService;

    #endregion

    public MainViewModel(
        IToolboxService toolboxService,
        IThemeService themeService,
        IDockWindowService dockWindowService,
        IPersistenceService persistenceService,
        ILogService<MainViewModel> logService,
        IEditorTabViewModelFactory editorFactory,
        IFileService fileService,
        IDialogService dialogService)
    {
        mToolboxService = toolboxService;
        mThemeService = themeService;
        mDockWindowService = dockWindowService;
        mPersistenceService = persistenceService;
        mLogService = logService;
        mEditorFactory = editorFactory;
        mFileService = fileService;
        mDialogService = dialogService;

        // Регистрируем стартовые панели
        mToolboxService.Register<PropertiesToolbox>();
        mToolboxService.Register<OutputToolbox>();

        mLogService.LogInformation("Приложение запущено.");

        // Подписки
        mDockWindowService.PropertyChanged += OnDocumentPropertyChanged;
        mThemeService.PropertyChanged += ThemePropertyChanged;
    }

    // --- Пробросы свойств для биндинга ---

    public ObservableCollection<DocumentViewModelBase> Documents => mDockWindowService.Documents;
    public DocumentViewModelBase? ActiveDocument => mDockWindowService.ActiveDocument;
    public ObservableCollection<ToolViewModelBase> Anchorables => mToolboxService.Anchorables;
    public IReadOnlyList<ThemeInfo> AvailableThemes => mThemeService.AvailableThemes;
    public string? CurrentThemeName => mThemeService.CurrentTheme?.Name;
    

    // --- Жизненный цикл ---

    public void OnWindowLoaded() => mPersistenceService.Restore();

    public void OnWindowClosed() => mPersistenceService.Save();

    // --- Обработчики ---

    private void OnDocumentPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IDockWindowService.ActiveDocument))
        {
            OnPropertyChanged(nameof(ActiveDocument));

            SaveFileCommand.NotifyCanExecuteChanged();
            CloseDocumentCommand.NotifyCanExecuteChanged();
        }
    }

    private void ThemePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(mThemeService.CurrentTheme))
        {
            OnPropertyChanged(nameof(CurrentThemeName));
            mLogService.LogInformation($"Тема изменена: {CurrentThemeName}");
        }

    }
    // --- Команды ---

    [RelayCommand]
    private void OpenNewDocument()
    {
        var doc = mEditorFactory.CreateNew("Новый документ", $"Содержимое документа.\r\nСоздан: {DateTime.Now:G}");

        mDockWindowService.OpenDocument(doc);
        mDockWindowService.ActivateDocument(doc);

        mLogService.LogInformation($"Открыт документ: {doc.Title}");
    }


    [RelayCommand]
    private void OpenFile() //=> mDocumentService.OpenFile();
    {
        var path = mFileService.ShowOpenDialog();
        
        if (path is null)
            return;

        // 1. Ищем среди открытых документов, приводя к EditorTabViewModel
        var existing = mDockWindowService.Documents
                                     .OfType<EditorTabViewModel>()
                                     .FirstOrDefault(d => d.FilePath == path);

        if (existing != null)
        {
            // Если уже открыт — активируем через сервис
            mDockWindowService.ActivateDocument(existing);
            mLogService.LogInformation($"Активирован существующий: {path}");
            return;
        }

        try
        {
            EditorTabViewModel doc = mEditorFactory.CreateFromFile(path);

            mDockWindowService.OpenDocument(doc);
            mDockWindowService.ActivateDocument(doc);

            mLogService.LogInformation($"Открыт файл: {path}");
        }
        catch (Exception ex)
        {
            mLogService.LogError("Не удалось открыть файл", ex);
            mDialogService.ShowInformation($"Не удалось открыть файл: {ex.Message}", "Ошибка");
        }
    }

    [RelayCommand(CanExecute = nameof(CanSaveOrClose))]
    private void SaveFile()
    {
        if (ActiveDocument is EditorTabViewModel editor)
            editor.SaveToFile();
    }

    [RelayCommand(CanExecute = nameof(CanSaveOrClose))]
    private void CloseDocument()
    {
        if (ActiveDocument is { } doc)
            mDockWindowService.CloseDocument(doc);
    }

    private bool CanSaveOrClose() => ActiveDocument is not null;

    [RelayCommand]
    private void ToggleProperties() => mToolboxService.Toggle<PropertiesToolbox>();

    [RelayCommand]
    private void ToggleOutput() => mToolboxService.Toggle<OutputToolbox>();

    [RelayCommand]
    private void Exit() => Application.Current.Shutdown();

    [RelayCommand]
    private void SetTheme(string? themeName)
    {
        if (string.IsNullOrEmpty(themeName)) 
            return;

        mThemeService.ApplyTheme(themeName);
    }
}