using AdamStudio.EventArguments;
using AvalonDock.Core;
using AvalonDock.Core.Events;
using System;

namespace AdamStudio.Services;

/// <summary>
/// Relay-сервис: перебрасывает события DockingManager в обычные .NET события.
/// Не знает ни про MainViewModel, ни про конкретные типы VM.
///
/// Любой сервис может подписаться на нужные события через +=.
/// </summary>
public interface IDockEventBinder
{
    event EventHandler<ActiveContentChangedEventArgs>? ActiveContentChanged;
    event EventHandler<DocumentCancelEventArgs>? DocumentClosing;
    event EventHandler<AnchorableEventArgs>? AnchorableHidden;
    event EventHandler<AnchorableEventArgs> AnchorableClosed;

}

public sealed class DockEventBinder : IDockEventBinder
{
    public event EventHandler<ActiveContentChangedEventArgs>? ActiveContentChanged;
    public event EventHandler<DocumentCancelEventArgs>? DocumentClosing;
    public event EventHandler<AnchorableEventArgs>? AnchorableHidden;
    public event EventHandler<AnchorableEventArgs> AnchorableClosed;
    private readonly IDockingManagerProvider mProvider;
    private IDockingManager? mDockingManager;


    public DockEventBinder(IDockingManagerProvider provider)
    {
        mProvider = provider;
        mProvider.ManagerChanged += OnManagerChanged;
    }

    private void OnManagerChanged(object? sender, EventArgs e)
    {
        Unsubscribe();
        TrySubscribe(mProvider.Manager);
    }

    private void TrySubscribe(IDockingManager? manager)
    {
        if (manager is null) 
            return;

        mDockingManager = manager;

        manager.ActiveContentChanged += OnActiveContentChanged;
        manager.DocumentClosing += OnDocumentClosing;
        manager.AnchorableHidden += OnAnchorableHidden;
        manager.AnchorableClosed += OnAnchorableClosed;
    }

    private void Unsubscribe()
    {
        if (mDockingManager is null) 
            return;

        mDockingManager.ActiveContentChanged -= OnActiveContentChanged;
        mDockingManager.DocumentClosing -= OnDocumentClosing;
        mDockingManager.AnchorableHidden -= OnAnchorableHidden;
        mDockingManager = null;
    }

    private void OnActiveContentChanged(object? sender, EventArgs e)
    {
        ActiveContentChanged?.Invoke(sender, new ActiveContentChangedEventArgs
        {
            NewContent = mDockingManager?.ActiveContent
        });
    }

    private void OnDocumentClosing(object? sender, DocumentCancelEventArgs e) => DocumentClosing?.Invoke(sender, e);
    private void OnAnchorableHidden(object? sender, AnchorableEventArgs e) => AnchorableHidden?.Invoke(sender, e);
    private void OnAnchorableClosed(object? sender, AnchorableEventArgs e) => AnchorableClosed?.Invoke(sender, e);    

}
