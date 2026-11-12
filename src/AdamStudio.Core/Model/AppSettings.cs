using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AdamStudio.Core.Model;

public sealed partial class AppSettings : ObservableObject
{
    // ===== ПРИЛОЖЕНИЕ =====

    [Description("Тема оформления: VS2013Light, VS2013Dark, VS2013Blue")]
    [ObservableProperty]
    public partial string ThemeName { get; set; } = "VS2013Light";

    // ===== ОКНО =====

    [Description("Ширина главного окна (пиксели)")]
    [ObservableProperty]
    public partial double WindowWidth { get; set; } = 1100;


    [Description("Высота главного окна (пиксели)")]
    [ObservableProperty]
    public partial double WindowHeight { get; set; } = 700;

    [Description("Развернуть окно на весь экран (true/false)")]
    [ObservableProperty]
    public partial bool IsMaximized { get; set; }

    // ===== РЕДАКТОР =====

    [Description("Шрифт редактора (например: Consolas, Cascadia Code)")]
    [ObservableProperty]
    public partial string EditorFontFamily { get; set; } = "Consolas";


    [Description("Размер шрифта редактора (8-72)")]
    [ObservableProperty]
    public partial int EditorFontSize { get; set; } = 14;


    [Description("Показывать номера строк (true/false)")]
    [ObservableProperty]
    public partial bool ShowLineNumbers { get; set; } = true;

    // ===== ЛОГИ: ИНФРАСТРУКТУРА =====

    [Description("Путь к файлу логов относительно папки приложения")]
    [ObservableProperty]
    public partial string LogFilePath { get; set; } = "logs/app.log";

    [Description("Количество дней хранения логов (после удаляются)")]
    [ObservableProperty]
    public partial int RetainedFileCountLimit { get; set; } = 14;

    [Description("Шаблон вывода логов в файл (Serilog outputTemplate)")]
    [ObservableProperty]
    public partial string FileLogTemplate { get; set; } = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}";

    // ===== ЛОГИ: УРОВНИ (runtime) =====

    [Description("Уровень логов в файл: Verbose, Debug, Information, Warning, Error")]
    [ObservableProperty]
    public partial string GlobalLogLevel { get; set; } = "Debug";

    [Description("Уровень логов в панель UI [Вывод]: Verbose, Debug, Information, Warning, Error")]
    [ObservableProperty]
    public partial string OutputLogLevel { get; set; } = "Information";
}