using AdamStudio.Core.AvalonDock.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace AdamStudio.Core.AvalonDock.Styles;

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
        if (item is AvalonToolViewModel)
            return ToolboxStyle;

        if (item is AvalonDocumentViewModel)
            return DocumentStyle;

        return base.SelectStyle(item, container);
    }
}
