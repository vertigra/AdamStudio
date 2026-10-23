using System;
using System.Windows;
using AvalonDock;

namespace AdamStudio.Services;

/// <summary>
/// Контракт для ViewModel, которая хочет получать уведомления о
/// событиях жизненного цикла окна (Loaded/Closed).
/// </summary>
public interface IWindowLifecycleAware
{
    void OnWindowLoaded();
    void OnWindowClosed();
}

/// <summary>
/// Сервис жизненного цикла окна. Подписывается на Loaded/Closed окна
/// и регистрирует DockingManager в IDockingManagerProvider.
/// MainWindow передаёт себя и DockingManager в этот сервис —
/// больше никакой логики в code-behind не остаётся.
/// </summary>
public interface IWindowLifecycleService
{
    /// <summary>Подписать окно на события жизненного цикла.</summary>
    void Attach(Window window, DockingManager dockManager);
}

public sealed class WindowLifecycleService : IWindowLifecycleService
{
    private readonly IDockingManagerProvider mDockingManagerProvider;

    public WindowLifecycleService(IDockingManagerProvider managerProvider)
    {
        mDockingManagerProvider = managerProvider;
    }

    public void Attach(Window window, DockingManager dockManager)
    {
        window.Loaded += OnLoaded;
        window.Closed += OnClosed;

        return;

        // --- Локальные обработчики — захватывают window и dockManager из замыкания ---

        void OnLoaded(object? sender, RoutedEventArgs e)
        {
            // Регистрируем DockingManager — теперь сервисы (LayoutSerializer,
            // DockEventBinder) смогут с ним работать.
            mDockingManagerProvider.Manager = dockManager;

            // Если VM реализует IWindowLifecycleAware — уведомляем о загрузке
            if (window.DataContext is IWindowLifecycleAware aware)
                aware.OnWindowLoaded();

            // Отписываемся — обработчик нужен только один раз
            window.Loaded -= OnLoaded;
        }

        void OnClosed(object? sender, EventArgs e)
        {       
            if (window.DataContext is IWindowLifecycleAware aware)
                aware.OnWindowClosed();

            // Чистим ссылку, чтобы не держать DockingManager в памяти
            mDockingManagerProvider.Manager = null;
        }
    }
}
