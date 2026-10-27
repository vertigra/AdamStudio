namespace AdamStudio.Core.Services;

/// <summary>
/// Единый сервис персистентности приложения. Инкапсулирует:
/// - сохранение/восстановление layout (layout.xml)
/// - сохранение/восстановление workspace (workspace.json — список документов
/// - debounced автосохранение layout при изменениях
///
/// MainViewModel делегирует сюда всё — никаких путей к файлам и сериализации в VM.
/// </summary>
public interface IPersistenceService
{
    /// <summary>
    /// Сообщает подписчикам что необходимо восстановить настройки
    /// </summary>
    event EventHandler? RestoreHappened;

    /// <summary>Восстановить всё состояние при старте приложения включая состояние, вызывает событие RestoreHappened</summary>
    void Restore();

    /// <summary>Сохранить всё состояние (выход, явная команда).</summary>
    void Save();

    /// <summary>Запланировать отложенное сохранение layout (debounce 500мс).</summary>
    void ScheduleLayoutSave();
}