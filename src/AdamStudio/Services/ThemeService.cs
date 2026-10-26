using AdamStudio.Model;
using AvalonDock.Themes;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace AdamStudio.Services;

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

    /// <summary>Применить тему напрямую.</summary>
    void ApplyTheme(ThemeInfo theme);
}


public sealed partial class ThemeService : ObservableObject, IThemeService
{
    private readonly IDockingManagerProvider mManagerProvider;
    private readonly ISettingsService mSettingsService;

    public ThemeService(IDockingManagerProvider managerProvider, ISettingsService settingsService, IAppPersistenceService appPersistenceService)
    {
        mManagerProvider = managerProvider;
        mSettingsService = settingsService;

        // Доступные темы. Добавьте свои при необходимости.
        AvailableThemes = new List<ThemeInfo>
        {
            new() { Name = "VS2013Light",  DisplayName = "VS2013: Светлая",  CreateTheme = () => new Vs2013LightTheme() },
            new() { Name = "VS2013Dark",   DisplayName = "VS2013: Тёмная",  CreateTheme = () => new Vs2013DarkTheme() },
            new() { Name = "VS2013Blue",   DisplayName = "VS2013: Синяя",   CreateTheme = () => new Vs2013BlueTheme() },
        }.AsReadOnly();

        appPersistenceService.RestoreHappened += RestoreHappened;
    }

    private void RestoreHappened(object? sender, System.EventArgs e)
    {
        RestoreSetting();
    }

    public IReadOnlyList<ThemeInfo> AvailableThemes { get; }

    [ObservableProperty]
    public partial ThemeInfo? CurrentTheme { get; private set; }

    partial void OnCurrentThemeChanged(ThemeInfo? value)
    {
        SaveSettings(value);
    }

    public void ApplyTheme(string name)
    {
        ThemeInfo? theme = AvailableThemes.FirstOrDefault(t => t.Name == name);
        
        if (theme is null) 
            return;

        ApplyTheme(theme);
    }

    public void ApplyTheme(ThemeInfo theme)
    {
        if (mManagerProvider.Manager is AvalonDock.DockingManager manager)
        {
            manager.Theme = theme.CreateTheme();
            CurrentTheme = theme;
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
