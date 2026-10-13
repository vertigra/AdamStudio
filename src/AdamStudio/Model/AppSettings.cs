namespace AdamStudio.Model
{
    /// <summary>Настройки приложения (тема и т.д.) — сохраняются в settings.json.</summary>
    public sealed class AppSettings
    {
        public string? ThemeName { get; set; } = "VS2013Light";
    }
}
