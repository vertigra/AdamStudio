using AdamStudio.Core.AvalonDock.EventArguments;
using AvalonDock.Core;
using AvalonDock.Core.Events;
using AvalonDock.Layout;
using Microsoft.Extensions.Logging;

namespace AdamStudio.Core.AvalonDock.Services;

public interface IDockEventBinder
{
    event EventHandler<ActiveContentChangedEventArgs>? ActiveContentChanged;
    event EventHandler<DocumentCancelEventArgs>? DocumentClosing;
    event EventHandler<AnchorableEventArgs>? AnchorableHidden;
    event EventHandler<AnchorableEventArgs>? AnchorableClosed;
    event EventHandler? LayoutChanged;
}

public sealed class DockEventBinder : IDockEventBinder
{
    public event EventHandler<ActiveContentChangedEventArgs>? ActiveContentChanged;
    public event EventHandler<DocumentCancelEventArgs>? DocumentClosing;
    public event EventHandler<AnchorableEventArgs>? AnchorableHidden;
    public event EventHandler<AnchorableEventArgs>? AnchorableClosed;
    public event EventHandler? LayoutChanged;
    
    private readonly IDockingManagerProvider mProvider;
    private readonly ILogger<DockEventBinder> mLoggerService;
    
    private IDockingManager? mDockingManager;
    private LayoutRoot? mLayoutRoot;

    public DockEventBinder(IDockingManagerProvider provider, ILogger<DockEventBinder> logger)
    {
        mProvider = provider;
        mLoggerService = logger;

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
        manager.LayoutChanged += OnLayoutManagerChanged;

        // Если Layout уже есть — подписываемся сразу
        TrySubscribeLayout((LayoutRoot?) manager.Layout);
    }

    private void OnLayoutManagerChanged(object? sender, EventArgs e)
    {
        // Layout только что установился (или заменился) — подписываемся заново
        TryUnsubscribeLayout();
        TrySubscribeLayout((LayoutRoot?) mDockingManager?.Layout);
    }

    private void TrySubscribeLayout(LayoutRoot? root)
    {
        if (root is null)
            return;

        mLayoutRoot = root;
        // Updated срабатывает при ЛЮБЫХ структурных изменениях во всём дереве:
        // перемещение, показ, скрытие, dock/undock, смена ширины
        root.Updated += OnLayoutUpdated;
    }

    private void TryUnsubscribeLayout()
    {
        if (mLayoutRoot is null)
            return;

        mLayoutRoot.Updated -= OnLayoutUpdated;
        mLayoutRoot = null;
    }

    
    private void Unsubscribe()
    {
        if (mDockingManager is null)
            return;

        mDockingManager.ActiveContentChanged -= OnActiveContentChanged;
        mDockingManager.DocumentClosing -= OnDocumentClosing;
        mDockingManager.AnchorableHidden -= OnAnchorableHidden;
        mDockingManager.AnchorableClosed -= OnAnchorableClosed;
        mDockingManager.LayoutChanged -= OnLayoutManagerChanged;

        TryUnsubscribeLayout();
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

    private void OnLayoutUpdated(object? sender, EventArgs e) => LayoutChanged?.Invoke(sender, e);
}