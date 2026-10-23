using AdamStudio.Model;
using CommunityToolkit.Mvvm.ComponentModel;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace AdamStudio.Services;

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
                return;

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
            JsonObject root;
            if (File.Exists(SettingsFilePath))
            {
                var json = File.ReadAllText(SettingsFilePath);
                root = JsonSerializer.Deserialize<JsonObject>(json) ?? [];
            }
            else
            {
                root = [];
            }

            var appNode = new JsonObject();
            
            var props = typeof(AppSettings).GetProperties();

            foreach (var prop in props)
            {
                var value = prop.GetValue(Current);
                appNode[prop.Name] = JsonSerializer.SerializeToNode(value, prop.PropertyType);
            }

            root["App"] = appNode;
            File.WriteAllText(SettingsFilePath, JsonSerializer.Serialize(root, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Ошибка сохранения настроек");
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
        
        Save();
    }

    private static void ApplyLogLevel(LoggingLevelSwitch levelSwitch, string level)
    {
        if (Enum.TryParse<LogEventLevel>(level, true, out var lvl))
            levelSwitch.MinimumLevel = lvl;
    }
}