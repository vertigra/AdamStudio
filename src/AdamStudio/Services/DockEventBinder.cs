using AdamStudio.EventArguments;
using AdamStudio.ViewModels;
using AvalonDock;
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
    /// <summary>Привязать MainViewModel к событиям DockingManager.</summary>
    //void Bind(MainViewModel viewModel);

    /// <summary>Отвязать MainViewModel (при закрытии приложения).</summary>
    //void Unbind();

    event EventHandler<ActiveContentChangedEventArgs>? ActiveContentChanged;
    event EventHandler<DocumentCancelEventArgs>? DocumentClosing;
    event EventHandler<DocumentEventArgs>? DocumentClosed;
    event EventHandler<AnchorableEventArgs>? AnchorableClosed;

    /// <summary>Активировать подписку на DockingManager (после его регистрации в IDockingManagerProvider).</summary>
    void Activate();
}

public sealed class DockEventBinder : IDockEventBinder
{
    public event EventHandler<ActiveContentChangedEventArgs>? ActiveContentChanged;
    public event EventHandler<DocumentCancelEventArgs>? DocumentClosing;
    public event EventHandler<DocumentEventArgs>? DocumentClosed;
    public event EventHandler<AnchorableEventArgs>? AnchorableClosed;

    private readonly IDockingManagerProvider mProvider;
    private IDockingManager? mDockingManager;


    public DockEventBinder(IDockingManagerProvider provider)
    {
        mProvider = provider;
        mProvider.ManagerChanged += OnManagerChanged;
    }

    public void Activate()
    {
        // Если менеджер уже зарегистрирован — подписываемся сразу.
        // Если нет — подписка произойдёт автоматически при ManagerChanged.
        TrySubscribe(mProvider.Manager);
    }

    private void OnManagerChanged(object? sender, EventArgs e)
    {
        // Менеджер сменился (например, окно закрылось) — переподписываемся
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
        manager.DocumentClosed += OnDocumentClosed;
        manager.AnchorableClosed += OnAnchorableClosed;
    }

    private void Unsubscribe()
    {
        if (mDockingManager is null) 
            return;

        mDockingManager.ActiveContentChanged -= OnActiveContentChanged;
        mDockingManager.DocumentClosing -= OnDocumentClosing;
        mDockingManager.DocumentClosed -= OnDocumentClosed;
        mDockingManager.AnchorableClosed -= OnAnchorableClosed;
        mDockingManager = null;
    }

    // --- Обработчики — делегируют в MainViewModel ---

    private void OnActiveContentChanged(object? sender, EventArgs e)
    {
        ActiveContentChanged?.Invoke(sender, new ActiveContentChangedEventArgs
        {
            NewContent = mDockingManager?.ActiveContent
        });
    }

    private void OnDocumentClosing(object? sender, DocumentCancelEventArgs e) => DocumentClosing?.Invoke(sender, e);

    private void OnDocumentClosed(object? sender, DocumentEventArgs e) => DocumentClosed?.Invoke(sender, e);

    private void OnAnchorableClosed(object? sender, AnchorableEventArgs e) => AnchorableClosed?.Invoke(sender, e);
}
