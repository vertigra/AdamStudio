using System;
using System.Collections.ObjectModel;
using AvalonDock.Layout;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AdamStudio.ViewModels;

/// <summary>
/// Панель «Вывод» — закрепляется снизу, логирует сообщения приложения.
/// </summary>
public sealed partial class OutputToolbox : ToolViewModelBase
{
    public ObservableCollection<string> LogLines { get; } = [];

    [ObservableProperty]
    private string _outputText = "Готов.\n";

    public OutputToolbox() : base("Вывод", "OutputTool")
    {
        PreferredSide = AnchorSide.Bottom;

    }

    public void AppendLine(string text)
    {
        LogLines.Add($"[{DateTime.Now:HH:mm:ss}] {text}");
        OutputText += $"[{DateTime.Now:HH:mm:ss}] {text}\n";
    }
}
