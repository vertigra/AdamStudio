using AdamStudio.ViewModels;
using AvalonDock.Layout;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace AdamStudio.Docking
{
    /// <summary>
    /// Стратегия размещения новых панелей (LayoutAnchorable) в классическом DockingManager.
    /// Используется через &lt;DockingManager.LayoutUpdateStrategy&gt; в XAML.
    ///
    /// Логика:
    /// 1. Ищем существующую ОТКРЫТУЮ панель на нужной стороне (Right/Left/Bottom/Top).
    ///    Если нашли — добавляем anchorable туда (как новую вкладку).
    /// 2. Если не нашли — создаём новую LayoutAnchorablePane на нужной стороне
    ///    и помещаем anchorable в неё. Это исправляет баг, когда после закрытия
    ///    панели крестиком новая панель добавлялась «не пойми куда».
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

            // 1. Ищем существующую ОТКРЫТУЮ панель на нужной стороне
            var existingPane = layout.Descendents()
                .OfType<LayoutAnchorablePane>()
                .FirstOrDefault(p => GetSide(p) == tool.PreferredSide && p.ChildrenCount > 0);

            if (existingPane != null)
            {
                existingPane.Children.Add(anchorable);
                anchorable.IsActive = true;
                return true;
            }

            // 2. Не нашли — создаём новую LayoutAnchorablePane на нужной стороне
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

        // --- Создание нижней панели ---

        private static bool CreateBottomPane(LayoutRoot layout, LayoutAnchorable anchorable)
        {
            var newPane = new LayoutAnchorablePane(anchorable)
            {
                DockHeight = new GridLength(180, GridUnitType.Pixel)
            };
            anchorable.IsActive = true;

            var rootPanel = layout.RootPanel;

            if (rootPanel.Orientation == Orientation.Vertical)
            {
                // Root уже вертикальный — просто добавляем снизу
                rootPanel.Children.Add(newPane);
            }
            else
            {
                // Root горизонтальный — оборачиваем его в новый вертикальный LayoutPanel:
                //   LayoutRoot.RootPanel = verticalPanel
                //     ├── [старый rootPanel]
                //     └── newPane (bottom)
                //
                // ВАЖНО: используем layout.RootPanel = ... (сеттер),
                // а не parent.Children.Remove/Insert — потому что у корневого
                // LayoutPanel родитель — это LayoutRoot, и через LayoutGroup<>
                // манипулировать им нельзя (что и приводило к «пропаданию» панелей).
                var verticalPanel = new LayoutPanel { Orientation = Orientation.Vertical };
                verticalPanel.Children.Add(rootPanel);
                verticalPanel.Children.Add(newPane);
                layout.RootPanel = verticalPanel;
            }

            return true;
        }

        // --- Создание верхней панели ---

        private static bool CreateTopPane(LayoutRoot layout, LayoutAnchorable anchorable)
        {
            var newPane = new LayoutAnchorablePane(anchorable)
            {
                DockHeight = new GridLength(180, GridUnitType.Pixel)
            };
            anchorable.IsActive = true;

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

        // --- Создание боковой панели (Left/Right) ---

        private static bool CreateSidePane(LayoutRoot layout, LayoutAnchorable anchorable, AnchorSide side)
        {
            var newPane = new LayoutAnchorablePane(anchorable)
            {
                DockWidth = new GridLength(260, GridUnitType.Pixel)
            };
            anchorable.IsActive = true;

            var rootPanel = layout.RootPanel;

            if (rootPanel.Orientation == Orientation.Horizontal)
            {
                // Root уже горизонтальный — просто добавляем слева/справа
                if (side == AnchorSide.Right)
                    rootPanel.Children.Add(newPane);
                else
                    rootPanel.Children.Insert(0, newPane);
            }
            else
            {
                // Root вертикальный — оборачиваем в горизонтальный LayoutPanel
                var horizontalPanel = new LayoutPanel { Orientation = Orientation.Horizontal };
                if (side == AnchorSide.Right)
                {
                    horizontalPanel.Children.Add(rootPanel);
                    horizontalPanel.Children.Add(newPane);
                }
                else // Left
                {
                    horizontalPanel.Children.Add(newPane);
                    horizontalPanel.Children.Add(rootPanel);
                }
                layout.RootPanel = horizontalPanel;
            }

            return true;
        }

        // --- Определение стороны панели ---

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

