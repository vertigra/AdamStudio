using AvalonDock.Core;
using System;

namespace AdamStudio.Services;

/// <summary>
/// Holder для DockingManager. MainWindow устанавливает ссылку при загрузке,
/// IDockEventBinder — подписывается на изменения.
/// </summary>
public interface IDockingManagerProvider
{
    IDockingManager? Manager { get; set; }
    event EventHandler? ManagerChanged;
}

public sealed class DockingManagerProvider : IDockingManagerProvider
{
    private IDockingManager? mDockingManager;

    public IDockingManager? Manager
    {
        get => mDockingManager;
        set
        {
            if (ReferenceEquals(mDockingManager, value)) 
                return;

            mDockingManager = value;
            ManagerChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public event EventHandler? ManagerChanged;
}
