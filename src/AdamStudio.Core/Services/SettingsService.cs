using AdamStudio.Core.Model;
using CommunityToolkit.Mvvm.ComponentModel;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System.ComponentModel;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace AdamStudio.Core.Services;

public interface ISettingsService : INotifyPropertyChanged
{
    event EventHandler<PropertyChangedEventArgs> SettingsPropertyChanged;

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
    public event EventHandler<PropertyChangedEventArgs>? SettingsPropertyChanged;
    private bool mIsLoading;

    public SettingsService()
    {
        SettingsDirectory = AppContext.BaseDirectory;
        SettingsFileName = "appsettings.json";
        SettingsFilePath = Path.Combine(SettingsDirectory, SettingsFileName);

        Current.PropertyChanged += OnSettingChanged;
        Load();
    }

    public string SettingsDirectory { get; }
    public string SettingsFileName { get; }
    public string SettingsFilePath { get; }

    public AppSettings Current { get; } = new();
    public LoggingLevelSwitch GlobalLevelSwitch { get; } = new(LogEventLevel.Debug);
    public LoggingLevelSwitch OutputPanelSwitch { get; } = new(LogEventLevel.Information);

    public void Load()
    {
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
            using var stream = new MemoryStream();
            using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });

            writer.WriteStartObject();
            writer.WriteStartObject("App");

            var props = typeof(AppSettings).GetProperties();
            foreach (var prop in props)
            {
                var descAttr = prop.GetCustomAttribute<DescriptionAttribute>();
                if (descAttr != null)
                    writer.WriteCommentValue($" {descAttr.Description} ");

                var value = prop.GetValue(Current);
                WriteJsonValue(writer, prop.Name, value, prop.PropertyType);
            }

            writer.WriteEndObject();
            writer.WriteEndObject();
            writer.Flush();

            var json = Encoding.UTF8.GetString(stream.ToArray());
            File.WriteAllText(SettingsFilePath, json);
        }
        catch (Exception ex) { Log.Error(ex, "Ошибка сохранения настроек"); }
    }

    private static void WriteJsonValue(Utf8JsonWriter writer, string name, object? value, Type type)
    {
        if (value == null)
        {
            writer.WriteNull(name);
            return;
        }

        if (type == typeof(string))
            writer.WriteString(name, (string)value);
        else if (type == typeof(int))
            writer.WriteNumber(name, (int)value);
        else if (type == typeof(double))
            writer.WriteNumber(name, (double)value);
        else if (type == typeof(bool))
            writer.WriteBoolean(name, (bool)value);
        else
            writer.WriteString(name, value.ToString());
    }

    private void OnSettingChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (mIsLoading)
            return;

        if (e.PropertyName == nameof(AppSettings.OutputLogLevel))
            ApplyLogLevel(OutputPanelSwitch, Current.OutputLogLevel);

        if (e.PropertyName == nameof(AppSettings.GlobalLogLevel))
            ApplyLogLevel(GlobalLevelSwitch, Current.GlobalLogLevel);

        SettingsPropertyChanged?.Invoke(this, e);
    }

    private static void ApplyLogLevel(LoggingLevelSwitch levelSwitch, string level)
    {
        if (Enum.TryParse<LogEventLevel>(level, true, out var lvl))
            levelSwitch.MinimumLevel = lvl;
    }
}