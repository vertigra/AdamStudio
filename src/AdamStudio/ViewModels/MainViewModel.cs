using AdamStudio.Model;
using AdamStudio.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;

namespace AdamStudio.ViewModels;

public sealed partial class MainViewModel : ObservableObject, IWindowLifecycleAware
{
    #region Services

    //private readonly IWorkspaceService mWorkspaceService;
    private readonly IToolboxService mToolboxService;
    private readonly IThemeService mThemeService;
    private readonly IDocumentService mDocumentService;
    private readonly IDockEventBinder mDockEventBinder;
    private readonly IAppPersistenceService mPersistenceService;
    private readonly ILogService<MainViewModel> mLogService;

    #endregion

    public MainViewModel(
        IToolboxService toolboxService,
        IThemeService themeService,
        IDocumentService documentService,
        IDockEventBinder dockEventBinder,
        IAppPersistenceService persistenceService,
        ILogService<MainViewModel> logService)
    {
        mToolboxService = toolboxService;
        mThemeService = themeService;
        mDocumentService = documentService;
        mDockEventBinder = dockEventBinder;
        mPersistenceService = persistenceService;
        mLogService = logService;

        // Регистрируем стартовые панели
        mToolboxService.Register<PropertiesToolbox>();
        mToolboxService.Register<OutputToolbox>();
        mToolboxService.Register<MainToolbox>();

        mLogService.LogInformation("Приложение запущено.");

        // Подписки
        mDocumentService.PropertyChanged += OnDocumentPropertyChanged;
        mDockEventBinder.LayoutChanged += OnLayoutChanged;
    }

    // --- Пробросы свойств для биндинга ---

    public ObservableCollection<EditorTabViewModel> Documents => mDocumentService.Documents;
    public EditorTabViewModel? ActiveDocument => mDocumentService.ActiveDocument;
    public ObservableCollection<ToolViewModelBase> Anchorables => mToolboxService.Anchorables;
    public IReadOnlyList<ThemeInfo> AvailableThemes => mThemeService.AvailableThemes;
    public string? CurrentThemeName => mThemeService?.CurrentTheme?.Name;

    // --- Жизненный цикл ---

    public void OnWindowLoaded()
    {
        // Восстанавливаем всё состояние (workspace + layout + setting)
        mPersistenceService.Restore();
    }

    public void OnWindowClosed() => mPersistenceService.Save();

    //вызывается из App.xaml и как будто бы лишнее, дублирует OnWindowClosed который вызывается из сервиса
    //public void OnApplicationExiting() => mPersistenceService.Save();

    // --- Обработчики ---

    private void OnDocumentPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IDocumentService.ActiveDocument))
        {
            OnPropertyChanged(nameof(ActiveDocument));
            SaveFileCommand.NotifyCanExecuteChanged();
            CloseDocumentCommand.NotifyCanExecuteChanged();
        }
    }

    private void OnLayoutChanged(object? sender, EventArgs e)
    {
        //mToolboxService.UpdateLastPositions();
        mPersistenceService.ScheduleLayoutSave();
    }

    // --- Команды ---

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

    [RelayCommand]
    private void SetTheme(string? themeName)
    {
        if (string.IsNullOrEmpty(themeName)) 
            return;

        mThemeService.ApplyTheme(themeName);
        mLogService.LogInformation($"Тема изменена: {themeName}");
    }
}