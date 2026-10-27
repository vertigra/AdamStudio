using AvalonDock.Themes;
using System;

namespace AdamStudio.Model
{
    /// <summary>Информация о теме для отображения в меню.</summary>
    public sealed class ThemeInfo
    {
        public string Name { get; init; } = string.Empty;
        public string DisplayName { get; init; } = string.Empty;
        public Func<Theme> CreateTheme { get; init; } = null!;
    }
}
