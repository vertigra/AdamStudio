using AdamStudio.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace AdamStudio.Styles;

/// <summary>
/// StyleSelector для LayoutItem — связывает свойства ViewModel
/// (Title, ContentId, ToolTip) с соответствующими свойствами LayoutItem.
/// Применяется через LayoutItemContainerStyleSelector.
/// </summary>
public sealed class PanesStyleSelector : StyleSelector
{
    public Style? ToolboxStyle { get; set; }
    public Style? DocumentStyle { get; set; }

    public override Style? SelectStyle(object item, DependencyObject container)
    {
        if (item is ToolViewModelBase)
            return ToolboxStyle;

        if (item is EditorTabViewModel)
            return DocumentStyle;

        return base.SelectStyle(item, container);
    }
}
