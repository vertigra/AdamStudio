using AdamStudio.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System.ComponentModel;
using System.Reflection;
using System.Text.Json;

namespace AdamStudio.Core.Services;

public interface ISettingsService : INotifyPropertyChanged
{
    string SettingsDirectory { get; }
    string SettingsFileName { get; }
    string SettingsFilePath { get; }

    AppSettings Current { get; }
    LoggingLevelSwitch GlobalLevelSwitch { get; }
    LoggingLevelSwitch OutputPanelSwitch { get; }

    void Load();
    void Save();
}

public sealed partial class SettingsService : ObservableObject, ISettingsService
{
    private bool mIsLoading;

    public SettingsService()
    {
        SettingsDirectory = AppContext.BaseDirectory;
        SettingsFileName = "appsettings.json";
        SettingsFilePath = Path.Combine(SettingsDirectory, SettingsFileName);

        Current.PropertyChanged += OnSettingChanged;
        Load();

        Log.Information("Test");
    }

    public string SettingsDirectory { get; }
    public string SettingsFileName { get; }
    public string SettingsFilePath { get; }

    public AppSettings Current { get; } = new();
    public LoggingLevelSwitch GlobalLevelSwitch { get; } = new(LogEventLevel.Debug);
    public LoggingLevelSwitch OutputPanelSwitch { get; } = new(LogEventLevel.Information);

    public void Load()
    {
        Log.Information("Load settings");

        try
        {
            mIsLoading = true;

            if (!File.Exists(SettingsFilePath))
            {
                ApplyLogLevel(OutputPanelSwitch, Current.OutputLogLevel);
                ApplyLogLevel(GlobalLevelSwitch, Current.GlobalLogLevel);
                return;
            }
                

            var json = File.ReadAllText(SettingsFilePath);
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("App", out var app))
                return;

            var props = typeof(AppSettings).GetProperties();

            foreach (var jsonProp in app.EnumerateObject())
            {
                var propInfo = props.FirstOrDefault(p => p.Name == jsonProp.Name);
                if (propInfo != null)
                {
                    var value = JsonSerializer.Deserialize(jsonProp.Value.GetRawText(), propInfo.PropertyType);
                    propInfo.SetValue(Current, value);
                }
            }

            ApplyLogLevel(OutputPanelSwitch, Current.OutputLogLevel);
            ApplyLogLevel(GlobalLevelSwitch, Current.GlobalLogLevel);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Ошибка загрузки настроек");
        }
        finally
        {
            mIsLoading = false;
        }
    }

    public void Save()
    {
        try
        {
            var tempFile = SettingsFilePath + ".tmp";

            // Пишем напрямую в файловый поток без промежуточного MemoryStream и string
            using (var stream = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
            {
                writer.WriteStartObject();
                writer.WriteStartObject("App");

                var props = typeof(AppSettings).GetProperties();
                foreach (var prop in props)
                {
                    // Пропускаем индексаторы, если они вдруг появятся в классе
                    if (prop.GetIndexParameters().Length > 0)
                        continue;

                    var descAttr = prop.GetCustomAttribute<DescriptionAttribute>();
                    if (descAttr != null)
                    {
                        // Защита от падения, если в описании встретится закрывающий тег комментария
                        var safeComment = descAttr.Description.Replace("*/", "* /");
                        writer.WriteCommentValue($" {safeComment} ");
                    }

                    var value = prop.GetValue(Current);
                    WriteJsonValue(writer, prop.Name, value);
                }

                writer.WriteEndObject();
                writer.WriteEndObject();
            } // Здесь автоматически сработает Dispose и Flush

            // Атомарная замена файла настроек (защита от повреждения файла при сбое)
            File.Move(tempFile, SettingsFilePath, overwrite: true);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Ошибка сохранения настроек");
        }
    }

    private static void WriteJsonValue(Utf8JsonWriter writer, string name, object? value)
    {
        if (value is null)
        {
            writer.WriteNull(name);
            return;
        }

        switch (value)
        {
            case string s:
                writer.WriteString(name, s);
                break;
            case Enum e:
                writer.WriteString(name, e.ToString());
                break;
            case int i:
                writer.WriteNumber(name, i);
                break;
            case long l:
                writer.WriteNumber(name, l);
                break;
            case decimal dec:
                writer.WriteNumber(name, dec);
                break;
            case double d when double.IsFinite(d):
                writer.WriteNumber(name, d);
                break;
            case float f when float.IsFinite(f):
                writer.WriteNumber(name, f);
                break;
            case double or float:
                writer.WriteNull(name);
                break;
            case bool b:
                writer.WriteBoolean(name, b);
                break;
            case DateTime dt:
                writer.WriteString(name, dt);
                break;
            case Guid g:
                writer.WriteString(name, g);
                break;
            default:
                writer.WriteString(name, Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture));
                break;
        }
    }



    private void OnSettingChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (mIsLoading)
            return;

        if (e.PropertyName == nameof(AppSettings.OutputLogLevel))
            ApplyLogLevel(OutputPanelSwitch, Current.OutputLogLevel);

        if (e.PropertyName == nameof(AppSettings.GlobalLogLevel))
            ApplyLogLevel(GlobalLevelSwitch, Current.GlobalLogLevel);
    }

    private static void ApplyLogLevel(LoggingLevelSwitch levelSwitch, string level)
    {
        if (Enum.TryParse<LogEventLevel>(level, true, out var lvl))
            levelSwitch.MinimumLevel = lvl;
    }
}