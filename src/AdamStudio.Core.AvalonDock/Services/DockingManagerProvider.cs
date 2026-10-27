using AvalonDock.Core;
using Microsoft.Extensions.Logging;

namespace AdamStudio.Core.AvalonDock.Services;

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
    private readonly ILogger<DockingManagerProvider> mLogger;

    private IDockingManager? mDockingManager;

    public DockingManagerProvider(ILogger<DockingManagerProvider> logger)
    {
        mLogger = logger;
    }

    
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
