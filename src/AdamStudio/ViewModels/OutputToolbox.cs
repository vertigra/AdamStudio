using AdamStudio.Core.Services;
using AvalonDock.Layout;
using System.Collections.ObjectModel;

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
        PreferredSide = AnchorSide.Right;
    }

    public ObservableCollection<string> LogLines => mLogService.LogLines;
}
