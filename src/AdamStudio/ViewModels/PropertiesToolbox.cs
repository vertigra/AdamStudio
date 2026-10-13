using AvalonDock.Layout;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AdamStudio.ViewModels;

/// <summary>
/// Панель «Свойства» — закрепляется справа.
/// </summary>
public sealed partial class PropertiesToolbox : ToolViewModelBase
{
    public PropertiesToolbox() : base("Свойства", "PropertiesTool")
    {
        PreferredSide = AnchorSide.Right;
    }

    [ObservableProperty]
    public partial string SelectedObjectName { get; set; } = "(не выбрано)";

    [ObservableProperty]
    public partial int ObjectCount { get; set; }


}
