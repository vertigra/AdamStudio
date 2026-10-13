using AvalonDock.Layout;

namespace AdamStudio.ViewModels
{
    public sealed partial class MainToolbox : ToolViewModelBase
    {
        public MainToolbox(): base("Панель инcтрументов", "MainTool")
        {
            PreferredSide = AnchorSide.Left;
        }
    }
}
