using AvalonDock.Layout;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AdamStudio.ViewModels;

/// <summary>
/// Базовый класс для панелей-инструментов (Anchorable) в классическом DockingManager.
/// Заменяет v5 ObservableToolboxBase — упрощён, добавляет PreferredSide
/// для размещения через ILayoutUpdateStrategy.
///
/// Примечание: не наследуется от ObservableToolboxBase, т.к. тот завязан на
/// IDockLayoutService (через IToolbox). С классическим DockingManager + 
/// AnchorablesSource нам нужна просто VM с INotifyPropertyChanged.
/// </summary>
public abstract partial class ToolViewModelBase : ObservableObject
{
    protected ToolViewModelBase(string title, string contentId)
    {
        Title = title;
        ContentId = contentId;
    }

    /// <summary>Идентификатор для сериализации layout.</summary>
    /// 
    [ObservableProperty]
    public partial string ContentId { get; set; }

    [ObservableProperty]
    public partial string Title { get; set; }
   

    /// <summary>Сторона докинга по умолчанию — используется LayoutUpdateStrategy-ом.</summary>
    [ObservableProperty]
    public partial AnchorSide PreferredSide { get; set; }

    /// <summary>Видимость/открытость панели.</summary>
    [ObservableProperty]
    public partial bool IsOpen { get; set;  }

    /// <summary>Скрыта ли панель. Биндится напрямую на LayoutAnchorableItem.IsHidden (TwoWay).</summary>
    [ObservableProperty]
    public partial bool IsHidden { get; set; }


    [ObservableProperty]
    public partial bool IsActive { get; set; }
}
