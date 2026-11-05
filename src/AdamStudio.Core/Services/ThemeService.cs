using AdamStudio.Core.AvalonDock.Model;
using AdamStudio.Core.AvalonDock.Services;
using AdamStudio.Model;
using AvalonDock;
using AvalonDock.Themes;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using System.ComponentModel;

namespace AdamStudio.Core.Services.AvalonDock;

/// <summary>
/// Управление темами AvalonDock. Хранит список доступных тем,
/// текущую тему и применяет её к DockingManager через IDockingManagerProvider.
///
/// Используется в MainViewModel для команды смены темы из меню «Вид → Темы».
/// </summary>
public interface IThemeService : INotifyPropertyChanged
{
    /// <summary>Список доступных тем для биндинга в меню.</summary>
    IReadOnlyList<ThemeInfo> AvailableThemes { get; }

    /// <summary>Текущая тема (для отметки галочкой в меню).</summary>
    ThemeInfo? CurrentTheme { get; }

    /// <summary>Применить тему по имени.</summary>
    void ApplyTheme(string name);
}


public sealed partial class ThemeService : ObservableObject, IThemeService
{
    private readonly IDockingManagerProvider mManagerProvider;
    private readonly ISettingsService mSettingsService;
    private readonly IThemeFactoryService mThemeFactory;
    private readonly ILogService<ThemeService> mLogger;

    public ThemeService(IDockingManagerProvider managerProvider, ISettingsService settingsService, IPersistenceService appPersistenceService, IThemeFactoryService themeFactory, ILogService<ThemeService> logger)
    {
        mManagerProvider = managerProvider;
        mSettingsService = settingsService;
        mThemeFactory = themeFactory;
        mLogger = logger;

        appPersistenceService.RestoreHappened += RestoreHappened;
    }

    public IReadOnlyList<ThemeInfo> AvailableThemes => mThemeFactory.AvailableThemes;
        
    [ObservableProperty]
    public partial ThemeInfo? CurrentTheme { get; private set; }

    partial void OnCurrentThemeChanged(ThemeInfo? value)
    {
        SaveSettings(value);
    }

    private void RestoreHappened(object? sender, EventArgs e)
    {
        RestoreSetting();
    }

    public void ApplyTheme(string name)
    {
        try
        {
            if (mManagerProvider.Manager is DockingManager manager)
                CurrentTheme = mThemeFactory.ApplyTheme(name, manager);
        }
        catch(Exception ex)
        {
            mLogger.LogError("Ошибка при изменении темы", ex);
        }
        
    }

    private void SaveSettings(ThemeInfo? theme)
    {
        mSettingsService.Current.ThemeName = theme?.Name ?? "VS2013Light";
    }

    private void RestoreSetting()
    {
        ApplyTheme(mSettingsService.Current.ThemeName);
    }

}
