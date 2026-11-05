using AdamStudio.Core.AvalonDock.Model;
using AvalonDock;
using AvalonDock.Themes;

namespace AdamStudio.Core.AvalonDock.Services
{
    public interface IThemeFactoryService
    {
        IReadOnlyList<ThemeInfo> AvailableThemes { get; }
        ThemeInfo? ApplyTheme(string name, DockingManager manager);
    }

    public class ThemeFactoryService : IThemeFactoryService
    {
        public IReadOnlyList<ThemeInfo> AvailableThemes => mThemeFactories.Keys
        .Select(k => new ThemeInfo
        {
            Name = k,
            DisplayName = k switch
            {
                "VS2013Light" => "VS2013: Светлая",
                "VS2013Dark" => "VS2013: Тёмная",
                "VS2013Blue" => "VS2013: Синяя",
                _ => k
            }
        })
        .ToList()
        .AsReadOnly();

        // Маппинг "имя" → "фабрика темы AvalonDock" — живёт в App, не в Core
        private readonly Dictionary<string, Func<Theme>> mThemeFactories = new()
        {

            ["VS2013Light"] = () => new Vs2013LightTheme(),
            ["VS2013Dark"] = () => new Vs2013DarkTheme(),
            ["VS2013Blue"] = () => new Vs2013BlueTheme(),
        };

        public ThemeInfo? ApplyTheme(string name, DockingManager manager)
        {

            if (!mThemeFactories.TryGetValue(name, out var factory))
                throw new ArgumentException("Selected theme unaviable");

            manager.Theme = factory();
            return AvailableThemes.FirstOrDefault(t => t.Name == name);
        }

    }
}
