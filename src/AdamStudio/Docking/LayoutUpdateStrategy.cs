using AdamStudio.ViewModels;
using AvalonDock.Layout;
using System.Linq;
using System.Windows.Controls;

namespace AdamStudio.Docking;

/// <summary>
/// Стратегия размещения новых панелей в классическом DockingManager.
/// Используется через &lt;DockingManager.LayoutUpdateStrategy&gt; в XAML.
///
/// Логика: для каждой новой панели (LayoutAnchorable) ищем существующую
/// LayoutAnchorablePane на нужной стороне (Right/Bottom/Left/Top) и добавляем
/// панель туда. Если не нашли — возвращаем false, AvalonDock сам решит, куда
/// её поместить (обычно в первый LayoutAnchorablePane из XAML).
/// </summary>
public sealed class LayoutUpdateStrategy : ILayoutUpdateStrategy
{
    // документы идут в LayoutDocumentPane по умолчанию
    public bool BeforeInsertDocument(LayoutRoot layout, LayoutDocument document, ILayoutContainer destinationContainer) => false; 

    public void AfterInsertDocument(LayoutRoot layout, LayoutDocument document) { }

    public bool BeforeInsertAnchorable(LayoutRoot layout, LayoutAnchorable anchorable, ILayoutContainer destinationContainer)
    {
        if (anchorable.Content is not ToolViewModelBase tool)
            return false;

        // Ищем существующую pane на нужной стороне
        var existingPane = layout.Descendents()
            .OfType<LayoutAnchorablePane>()
            .FirstOrDefault(p => GetSide(p) == tool.PreferredSide);

        if (existingPane != null)
        {
            existingPane.Children.Add(anchorable);
            anchorable.IsActive = true;
            return true;
        }

        // Не нашли — пусть AvalonDock разместит сам (вернём false)
        return false;
    }

    public void AfterInsertAnchorable(LayoutRoot layout, LayoutAnchorable anchorable) { }

    public bool InsertAnchorable(LayoutRoot layout, LayoutAnchorable anchorable, ILayoutContainer destinationContainer) => BeforeInsertAnchorable(layout, anchorable, destinationContainer);

    // --- Определение стороны панели ---

    private static AnchorSide GetSide(LayoutAnchorablePane pane)
    {
        if (pane.Root == null) return AnchorSide.Right;

        // Вложенные в LayoutPanel — определяем по позиции и ориентации родителя
        LayoutPanel? parentPanel = pane.Parent as LayoutPanel;
        if (parentPanel == null) return AnchorSide.Right;

        var idx = parentPanel.Children.IndexOf(pane);

        if (parentPanel.Orientation == Orientation.Horizontal)
            return idx == 0 ? AnchorSide.Left : AnchorSide.Right;

        // Vertical
        return idx == 0 ? AnchorSide.Top : AnchorSide.Bottom;
    }
}
