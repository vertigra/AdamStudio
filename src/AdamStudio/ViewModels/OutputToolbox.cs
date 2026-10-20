using System;
using System.Collections.ObjectModel;
using AdamStudio.Services;
using AvalonDock.Layout;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AdamStudio.ViewModels;

/// <summary>
/// Панель «Вывод» — закрепляется снизу, логирует сообщения приложения.
/// </summary>
public sealed partial class OutputToolbox : ToolViewModelBase
{

    #region Services

    private readonly ILogService<OutputToolbox> mLogService;

    #endregion

    public OutputToolbox(ILogService<OutputToolbox> logService) : base("Вывод", "OutputTool")
    {
        mLogService = logService;
        PreferredSide = AnchorSide.Bottom;
    }

    public ObservableCollection<string> LogLines => mLogService.LogLines;
}
