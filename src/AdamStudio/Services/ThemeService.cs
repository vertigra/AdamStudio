using AdamStudio.Model;
using AvalonDock.Themes;
using System.Collections.Generic;
using System.Linq;

namespace AdamStudio.Services;

/// <summary>
/// Управление темами AvalonDock. Хранит список доступных тем,
/// текущую тему и применяет её к DockingManager через IDockingManagerProvider.
///
/// Используется в MainViewModel для команды смены темы из меню «Вид → Темы».
/// </summary>
public interface IThemeService
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


public sealed class ThemeService : IThemeService
{
    private readonly IDockingManagerProvider mManagerProvider;
    private ThemeInfo? mCurrentTheme;

    public ThemeService(IDockingManagerProvider managerProvider)
    {
        mManagerProvider = managerProvider;

        // Доступные темы. Добавьте свои при необходимости.
        AvailableThemes = new List<ThemeInfo>
        {
            new() { Name = "VS2013Light",  DisplayName = "VS2013: Светлая",  CreateTheme = () => new Vs2013LightTheme() },
            new() { Name = "VS2013Dark",   DisplayName = "VS2013: Тёмная",  CreateTheme = () => new Vs2013DarkTheme() },
            new() { Name = "VS2013Blue",   DisplayName = "VS2013: Синяя",   CreateTheme = () => new Vs2013BlueTheme() },
        }.AsReadOnly();

        // По умолчанию — Light
        mCurrentTheme = AvailableThemes[0];
    }

    public IReadOnlyList<ThemeInfo> AvailableThemes { get; }

    public ThemeInfo? CurrentTheme => mCurrentTheme;

    public void ApplyTheme(string name)
    {
        var theme = AvailableThemes.FirstOrDefault(t => t.Name == name);
        
        if (theme is null) 
            return;

        ApplyTheme(theme);
    }

    public void ApplyTheme(ThemeInfo theme)
    {
        if (mManagerProvider.Manager is AvalonDock.DockingManager manager)
        {
            manager.Theme = theme.CreateTheme();
            mCurrentTheme = theme;
        }
    }
}
