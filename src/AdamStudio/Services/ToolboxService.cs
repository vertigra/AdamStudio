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
    private readonly IServiceProvider _services;
    private readonly IDockingManagerProvider _managerProvider;

    public ToolboxService(IServiceProvider services, IDockingManagerProvider managerProvider)
    {
        _services = services;
        _managerProvider = managerProvider;
    }

    public ObservableCollection<ToolViewModelBase> Anchorables { get; } = new();

    public T Register<T>() where T : ToolViewModelBase
    {
        var toolbox = _services.GetRequiredService<T>();
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

        // Есть в коллекции — переключаем видимость
        if (toolbox.IsHidden)
            ShowInternal(toolbox);
        else
            HideInternal(toolbox);
    }

    public void Show<T>() where T : ToolViewModelBase
    {
        var toolbox = Get<T>() ?? Register<T>();
        ShowInternal(toolbox);
    }

    public void Hide<T>() where T : ToolViewModelBase
    {
        var toolbox = Get<T>();
        
        if (toolbox is null) 
            return;
        HideInternal(toolbox);
    }

    // --- Внутренняя логика ---

    private void ShowInternal(ToolViewModelBase toolbox)
    {
        var anchorable = FindLayoutAnchorable(toolbox.ContentId);
        if (anchorable != null && anchorable.IsHidden)
        {
            // Скрыта — показываем (появится там же, где была)
            anchorable.Show();
        }
        toolbox.IsHidden = false;
        toolbox.IsActive = true;
    }

    private void HideInternal(ToolViewModelBase toolbox)
    {
        var anchorable = FindLayoutAnchorable(toolbox.ContentId);
        if (anchorable != null && !anchorable.IsHidden)
        {
            anchorable.Hide();
        }
        toolbox.IsHidden = true;
    }

    private LayoutAnchorable? FindLayoutAnchorable(string contentId)
    {
        var manager = _managerProvider.Manager;
        if (manager is null) return null;

        return manager.Layout?.Descendents()
            .OfType<LayoutAnchorable>()
            .FirstOrDefault(a => a.Content is ToolViewModelBase vm && vm.ContentId == contentId);
    }
}