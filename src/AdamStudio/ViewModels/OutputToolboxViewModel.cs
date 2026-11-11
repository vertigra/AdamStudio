using AdamStudio.Core.AvalonDock.BaseViewModels;
using AdamStudio.Core.Services;
using AvalonDock.Layout;
using System.Collections.ObjectModel;

namespace AdamStudio.ViewModels;

/// <summary>
/// Панель «Вывод» — закрепляется снизу, логирует сообщения приложения.
/// </summary>
public sealed partial class OutputToolboxViewModel : ToolViewModelBase
{

    #region Services

    private readonly ILogService<OutputToolboxViewModel> mLogService;

    #endregion

    public OutputToolboxViewModel(ILogService<OutputToolboxViewModel> logService) : base("Вывод", nameof(OutputToolboxViewModel))
    {
        mLogService = logService;
        PreferredSide = AnchorSide.Right;
    }

    public ObservableCollection<string> LogLines => mLogService.LogLines;
}
