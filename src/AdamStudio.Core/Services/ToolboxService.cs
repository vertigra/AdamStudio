using AdamStudio.Core.AvalonDock.ViewModels;
using AdamStudio.Core.AvalonDock.Services;
using AvalonDock.Layout;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;

namespace AdamStudio.Core.Services;

public interface IToolboxService
{
    ObservableCollection<AvalonToolViewModel> Anchorables { get; }
    T Register<T>() where T : AvalonToolViewModel;
    T? Get<T>() where T : AvalonToolViewModel;
    void Toggle<T>() where T : AvalonToolViewModel;
    void Show<T>() where T : AvalonToolViewModel;
    void Hide<T>() where T : AvalonToolViewModel;
    void UpdateLastPositions();
}

public sealed class ToolboxService : IToolboxService
{
    private readonly IServiceProvider mServices;
    private readonly IDockingManagerProvider mManagerProvider;
    private readonly Dictionary<string, LayoutAnchorablePane> mLastPanes = [];
    private readonly IDockEventBinder mDockEventBinder;

    public ToolboxService(IServiceProvider services, IDockingManagerProvider managerProvider, IDockEventBinder dockEventBinder)
    {
        mServices = services;
        mManagerProvider = managerProvider;
        mDockEventBinder = dockEventBinder;

        mDockEventBinder.LayoutChanged += OnLayoutChanged;
    }

    private void OnLayoutChanged(object? sender, EventArgs e)
    {
        UpdateLastPositions();
    }

    public ObservableCollection<AvalonToolViewModel> Anchorables { get; } = [];

    public T Register<T>() where T : AvalonToolViewModel
    {
        var toolbox = mServices.GetRequiredService<T>();
        if (!Anchorables.Contains(toolbox))
            Anchorables.Add(toolbox);

        return toolbox;
    }

    public T? Get<T>() where T : AvalonToolViewModel => Anchorables.OfType<T>().FirstOrDefault();

    public void Toggle<T>() where T : AvalonToolViewModel
    {
        var toolbox = Get<T>();
        if (toolbox is null)
        {
            // Нет в коллекции — добавляем (AvalonDock создаст и покажет)
            Register<T>();
            return;
        }

        var anchorable = FindLayoutAnchorable(toolbox.ContentId);
        if (anchorable is null) 
            return;

        // Читаем состояние напрямую из AvalonDock (источник истины)
        if (anchorable.IsHidden)
            ShowInternal(toolbox, anchorable);
        else
            HideInternal(toolbox, anchorable);
    }

    public void Show<T>() where T : AvalonToolViewModel
    {
        var toolbox = Get<T>() ?? Register<T>();
        var anchorable = FindLayoutAnchorable(toolbox.ContentId);

        if (anchorable != null)
            ShowInternal(toolbox, anchorable);
    }

    public void Hide<T>() where T : AvalonToolViewModel
    {
        var toolbox = Get<T>();
        if (toolbox is null) 
            return;

        var anchorable = FindLayoutAnchorable(toolbox.ContentId);

        if (anchorable != null) 
            HideInternal(toolbox, anchorable);
    }

    /// <summary>
    /// Запомнить текущие позиции всех открытых панелей.
    /// Вызывать при событии LayoutChanged (после любого перемещения панелей).
    /// </summary>
    public void UpdateLastPositions()
    {
        foreach (var toolbox in Anchorables)
        {
            var anchorable = FindLayoutAnchorable(toolbox.ContentId);
            if (anchorable?.Parent is LayoutAnchorablePane pane && pane.Root != null)
            {
                mLastPanes[toolbox.ContentId] = pane;
            }
        }
    }


    private void ShowInternal(AvalonToolViewModel toolbox, LayoutAnchorable anchorable)
    {
        // Сбрасываем PreviousContainer — не доверяем ему
        if (anchorable is ILayoutPreviousContainer prev)
            prev.PreviousContainer = null;

        // Если есть запомненная pane и она жива (в дереве) — добавляем туда
        if (mLastPanes.TryGetValue(toolbox.ContentId, out var pane) && pane.Root != null)
        {
            if (anchorable.Parent is LayoutAnchorablePane oldParent)
                oldParent.Children.Remove(anchorable);

            pane.Children.Add(anchorable);
        }

        anchorable.Show();
        anchorable.IsActive = true;
    }

    private void HideInternal(AvalonToolViewModel toolbox, LayoutAnchorable anchorable)
    {
        // Перед скрытием запоминаем текущую pane
        if (anchorable.Parent is LayoutAnchorablePane pane && pane.Root != null)
            mLastPanes[toolbox.ContentId] = pane;

        anchorable.Hide();
    }

    private LayoutAnchorable? FindLayoutAnchorable(string contentId)
    {
        var manager = mManagerProvider.Manager;
        if (manager is null) return null;

        return manager.Layout?.Descendents()
            .OfType<LayoutAnchorable>()
            .FirstOrDefault(a => a.Content is AvalonToolViewModel vm && vm.ContentId == contentId);
    }
}