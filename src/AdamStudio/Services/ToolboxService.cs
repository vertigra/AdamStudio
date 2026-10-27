using AdamStudio.Core.AvalonDock.Services;
using AdamStudio.ViewModels;
using AvalonDock.Layout;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace AdamStudio.Services;

public interface IToolboxService
{
    ObservableCollection<ToolViewModelBase> Anchorables { get; }
    T Register<T>() where T : ToolViewModelBase;
    T? Get<T>() where T : ToolViewModelBase;
    void Toggle<T>() where T : ToolViewModelBase;
    void Show<T>() where T : ToolViewModelBase;
    void Hide<T>() where T : ToolViewModelBase;
    void UpdateLastPositions();
}

public sealed class ToolboxService : IToolboxService
{
    private readonly IServiceProvider mServices;
    private readonly IDockingManagerProvider mManagerProvider;
    private readonly Dictionary<string, LayoutAnchorablePane> _lastPanes = [];
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

    public ObservableCollection<ToolViewModelBase> Anchorables { get; } = [];

    public T Register<T>() where T : ToolViewModelBase
    {
        var toolbox = mServices.GetRequiredService<T>();
        if (!Anchorables.Contains(toolbox))
            Anchorables.Add(toolbox);
        return toolbox;
    }

    public T? Get<T>() where T : ToolViewModelBase => Anchorables.OfType<T>().FirstOrDefault();

    public void Toggle<T>() where T : ToolViewModelBase
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

    public void Show<T>() where T : ToolViewModelBase
    {
        var toolbox = Get<T>() ?? Register<T>();
        var anchorable = FindLayoutAnchorable(toolbox.ContentId);

        if (anchorable != null)
            ShowInternal(toolbox, anchorable);
    }

    public void Hide<T>() where T : ToolViewModelBase
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
                _lastPanes[toolbox.ContentId] = pane;
            }
        }
    }


    private void ShowInternal(ToolViewModelBase toolbox, LayoutAnchorable anchorable)
    {
        // Сбрасываем PreviousContainer — не доверяем ему
        if (anchorable is ILayoutPreviousContainer prev)
            prev.PreviousContainer = null;

        // Если есть запомненная pane и она жива (в дереве) — добавляем туда
        if (_lastPanes.TryGetValue(toolbox.ContentId, out var pane) && pane.Root != null)
        {
            if (anchorable.Parent is LayoutAnchorablePane oldParent)
                oldParent.Children.Remove(anchorable);

            pane.Children.Add(anchorable);
        }

        anchorable.Show();
        anchorable.IsActive = true;
    }

    private void HideInternal(ToolViewModelBase toolbox, LayoutAnchorable anchorable)
    {
        // Перед скрытием запоминаем текущую pane
        if (anchorable.Parent is LayoutAnchorablePane pane && pane.Root != null)
            _lastPanes[toolbox.ContentId] = pane;

        anchorable.Hide();
    }

    private LayoutAnchorable? FindLayoutAnchorable(string contentId)
    {
        var manager = mManagerProvider.Manager;
        if (manager is null) return null;

        return manager.Layout?.Descendents()
            .OfType<LayoutAnchorable>()
            .FirstOrDefault(a => a.Content is ToolViewModelBase vm && vm.ContentId == contentId);
    }
}