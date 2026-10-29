using AvalonDock.Core;
using AvalonDock.Serializer.Xml;
using System.IO;
using System.Xml;

namespace AdamStudio.Core.AvalonDock.Services
{
    /// <summary>
    /// Сериализация layout AvalonDock в XML.
    /// Сохраняет: положение панелей, ширины/высоты, floating-окна,
    /// состояние IsAutoHidden, порядок вкладок, активную вкладку.
    ///
    /// Использует XmlLayoutSerializer из пакета Dirkster.AvalonDock.Serializer.Xml.
    /// В v5 API изменился по сравнению с v4:
    ///   - Конструктор принимает IDockingManager (не DockingManager)
    ///   - Serialize/Deserialize принимают Stream/TextWriter/string (не XmlTextWriter)
    ///   - LayoutSerializationCallback — это событие (+=), не свойство (=)
    ///   - Для отмены элемента: args.UnresolvedContentHandling = UnresolvedContentHandling.Remove
    ///     (вместо args.Cancel = true в v4)
    /// </summary>
    public interface ILayoutSerializerService
    {
        /// <summary>Сохранить layout в файл.</summary>
        void Serialize(IDockingManager manager, string path);

        /// <summary>Восстановить layout из файла. Возвращает false, если файл не существует или повреждён.</summary>
        bool Deserialize(IDockingManager manager, string path);
    }

    public sealed class LayoutSerializerService : ILayoutSerializerService
    {
        public void Serialize(IDockingManager manager, string path)
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            var serializer = new XmlLayoutSerializer(manager);
            serializer.Serialize(path);
        }

        public bool Deserialize(IDockingManager manager, string path)
        {
            if (!File.Exists(path))
                return false;

            try
            {
                var serializer = new XmlLayoutSerializer(manager);

                // В v5 это событие — подписка через +=, не присваивание.
                // Колбэк вызывается для каждого элемента layout при десериализации.
                // args.Content — объект VM, найденный в DocumentsSource/AnchorablesSource
                //                по ContentId. Если null — VM не найдена.
                // args.UnresolvedContentHandling:
                //   - Remove (по умолчанию) — элемент удаляется из layout
                //   - Hide — элемент остаётся скрытым
                EventHandler<LayoutSerializationCallbackEventArgs> callback = (_, args) =>
                {
                    if (args.Content is null)
                        args.UnresolvedContentHandling = UnresolvedContentHandling.Remove;
                };
                serializer.LayoutSerializationCallback += callback;

                try
                {
                    serializer.Deserialize(path);
                }
                finally
                {
                    serializer.LayoutSerializationCallback -= callback;
                }

                return true;
            }
            catch (IOException) { return false; }
            catch (XmlException) { return false; }
            catch (InvalidOperationException) { return false; }
        }
    }
}