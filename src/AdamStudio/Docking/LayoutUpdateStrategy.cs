using AdamStudio.ViewModels;
using AvalonDock.Layout;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace AdamStudio.Docking
{
    /// <summary>
    /// Стратегия размещения новых панелей при добавлении в AnchorablesSource.
    /// Используется ТОЛЬКО для первичного позиционирования (при старте или первой регистрации).
    /// Для Show/Hide во время работы используется ToolboxService.
    /// </summary>
    public sealed class LayoutUpdateStrategy : ILayoutUpdateStrategy
    {
        public bool BeforeInsertDocument(LayoutRoot layout, LayoutDocument document, ILayoutContainer destinationContainer)
            => false;

        public void AfterInsertDocument(LayoutRoot layout, LayoutDocument document) { }

        public bool BeforeInsertAnchorable(LayoutRoot layout, LayoutAnchorable anchorable, ILayoutContainer destinationContainer)
        {
            if (anchorable.Content is not ToolViewModelBase tool)
                return false;

            // Ищем существующую ОТКРЫТУЮ pane на нужной стороне
            var existingPane = layout.Descendents()
                .OfType<LayoutAnchorablePane>()
                .FirstOrDefault(p => GetSide(p) == tool.PreferredSide && p.ChildrenCount > 0);

            /*if (existingPane != null)
            {
                existingPane.Children.Add(anchorable);
                return true;
            }*/

            // Не нашли — создаём новую на нужной стороне
            return tool.PreferredSide switch
            {
                AnchorSide.Right => CreateSidePane(layout, anchorable, AnchorSide.Right),
                AnchorSide.Left => CreateSidePane(layout, anchorable, AnchorSide.Left),
                AnchorSide.Bottom => CreateBottomPane(layout, anchorable),
                AnchorSide.Top => CreateTopPane(layout, anchorable),
                _ => false
            };
        }

        public void AfterInsertAnchorable(LayoutRoot layout, LayoutAnchorable anchorable) { }

        public bool InsertAnchorable(LayoutRoot layout, LayoutAnchorable anchorable, ILayoutContainer destinationContainer)
            => BeforeInsertAnchorable(layout, anchorable, destinationContainer);

        // --- Создание панелей ---

        private static bool CreateSidePane(LayoutRoot layout, LayoutAnchorable anchorable, AnchorSide side)
        {
            var newPane = new LayoutAnchorablePane(anchorable)
            {
                DockWidth = new GridLength(260, GridUnitType.Pixel)
            };

            var rootPanel = layout.RootPanel;

            if (rootPanel.Orientation == Orientation.Horizontal)
            {
                if (side == AnchorSide.Right)
                    rootPanel.Children.Add(newPane);
                else
                    rootPanel.Children.Insert(0, newPane);
            }
            else
            {
                var horizontalPanel = new LayoutPanel { Orientation = Orientation.Horizontal };
                if (side == AnchorSide.Right)
                {
                    horizontalPanel.Children.Add(rootPanel);
                    horizontalPanel.Children.Add(newPane);
                }
                else
                {
                    horizontalPanel.Children.Add(newPane);
                    horizontalPanel.Children.Add(rootPanel);
                }
                layout.RootPanel = horizontalPanel;
            }

            return true;
        }

        private static bool CreateBottomPane(LayoutRoot layout, LayoutAnchorable anchorable)
        {
            var newPane = new LayoutAnchorablePane(anchorable)
            {
                DockHeight = new GridLength(180, GridUnitType.Pixel)
            };

            var rootPanel = layout.RootPanel;

            if (rootPanel.Orientation == Orientation.Vertical)
            {
                rootPanel.Children.Add(newPane);
            }
            else
            {
                var verticalPanel = new LayoutPanel { Orientation = Orientation.Vertical };
                verticalPanel.Children.Add(rootPanel);
                verticalPanel.Children.Add(newPane);
                layout.RootPanel = verticalPanel;
            }

            return true;
        }

        private static bool CreateTopPane(LayoutRoot layout, LayoutAnchorable anchorable)
        {
            var newPane = new LayoutAnchorablePane(anchorable)
            {
                DockHeight = new GridLength(180, GridUnitType.Pixel)
            };

            var rootPanel = layout.RootPanel;

            if (rootPanel.Orientation == Orientation.Vertical)
            {
                rootPanel.Children.Insert(0, newPane);
            }
            else
            {
                var verticalPanel = new LayoutPanel { Orientation = Orientation.Vertical };
                verticalPanel.Children.Add(newPane);
                verticalPanel.Children.Add(rootPanel);
                layout.RootPanel = verticalPanel;
            }

            return true;
        }

        private static AnchorSide GetSide(LayoutAnchorablePane pane)
        {
            if (pane.Root == null)
                return AnchorSide.Right;

            var parentPanel = pane.Parent as LayoutPanel;
            if (parentPanel == null)
                return AnchorSide.Right;

            var idx = parentPanel.Children.IndexOf(pane);
            if (parentPanel.Orientation == Orientation.Horizontal)
                return idx == 0 ? AnchorSide.Left : AnchorSide.Right;

            return idx == 0 ? AnchorSide.Top : AnchorSide.Bottom;
        }
    }
}

