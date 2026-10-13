using AdamStudio.ViewModels;
using AvalonDock.Layout;
using Microsoft.Extensions.DependencyInjection;
using System;
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
}

public sealed class ToolboxService : IToolboxService
{
    private readonly IServiceProvider mServices;
    private readonly IDockingManagerProvider mDockingManagerProvider;

    public ToolboxService(IServiceProvider services, IDockingManagerProvider managerProvider)
    {
        mServices = services;
        mDockingManagerProvider = managerProvider;
    }

    public ObservableCollection<ToolViewModelBase> Anchorables { get; } = [];

    public T Register<T>() where T : ToolViewModelBase
    {
        var toolbox = mServices.GetRequiredService<T>();

        if (!Anchorables.Contains(toolbox))
            Anchorables.Add(toolbox);

        return toolbox;
    }

    public T? Get<T>() where T : ToolViewModelBase
    {
        return Anchorables.OfType<T>().FirstOrDefault();
    }

    public void Toggle<T>() where T : ToolViewModelBase
    {
        var toolbox = Get<T>();
        if (toolbox is null)
        {
            // Панель не зарегистрирована — добавляем в AnchorablesSource.
            // AvalonDock создаст LayoutAnchorable и покажет её.
            toolbox = Register<T>();
            //toolbox.IsOpen = true;
            return;
        }

        // Панель есть в коллекции — ищем её LayoutAnchorable в дереве
        var anchorable = FindLayoutAnchorable(toolbox.ContentId);
        if (anchorable is null)
        {
            // LayoutAnchorable был удалён из дерева (пользователь закрыл крестиком)
            // Удаляем VM из Anchorables и регистрируем заново —
            // AvalonDock создаст новый LayoutAnchorable
            Anchorables.Remove(toolbox);
            Register<T>();
            toolbox.IsOpen = !toolbox.IsOpen;
            return;
        }

        // Управляем видимостью через Show()/Hide() — сохраняет позицию панели
        if (anchorable.IsHidden)
        {
            anchorable.Show();
            toolbox.IsOpen = true;
        }
        else
        {
            anchorable.Hide();
            toolbox.IsOpen = false;
        }
    }

    public void Show<T>() where T : ToolViewModelBase
    {
        var toolbox = Get<T>() ?? Register<T>();
        var anchorable = FindLayoutAnchorable(toolbox.ContentId);
        if (anchorable != null)
        {
            anchorable.Show();
            toolbox.IsOpen = true;
        }
    }

    public void Hide<T>() where T : ToolViewModelBase
    {
        var toolbox = Get<T>();
        if (toolbox is null) return;
        var anchorable = FindLayoutAnchorable(toolbox.ContentId);

        if (anchorable != null)
        {
            anchorable.Hide();
            toolbox.IsOpen = false;
        }
    }

    /// <summary>
    /// Найти LayoutAnchorable в дереве layout по ContentId.
    /// LayoutAnchorable.Content хранит нашу VM, у VM есть ContentId.
    /// </summary>
    private LayoutAnchorable? FindLayoutAnchorable(string contentId)
    {
        var manager = mDockingManagerProvider.Manager;
        if (manager is null) return null;

        // DockingManager.Layout — это LayoutRoot
        // Ищем среди всех LayoutAnchorable в дереве
        return manager.Layout?.Descendents()
            .OfType<LayoutAnchorable>()
            .FirstOrDefault(a => a.Content is ToolViewModelBase vm && vm.ContentId == contentId);
    }
}