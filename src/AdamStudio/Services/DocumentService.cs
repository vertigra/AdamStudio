using AdamStudio.EventArguments;
using AdamStudio.ViewModels;
using AvalonDock.Core.Events;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;

namespace AdamStudio.Services
{
    /// <summary>
    /// Сервис управления документами. Единственный источник правды:
    /// хранит коллекцию Documents, ActiveDocument, и инкапсулирует
    /// все операции (создать/открыть/сохранить/закрыть).
    ///
    /// Используется из:
    ///   - MainViewModel (команды меню)
    ///   - Любого Toolbox-а (например, FileExplorerToolbox по двойному клику)
    ///   - Drag-and-drop обработчиков
    ///   - Командной строки
    /// </summary>
    public interface IDocumentService : INotifyPropertyChanged
    {
        /// <summary>
        /// Переменная хранит номер документа и сколько всего вкладок открыто
        /// </summary>
        public int NewDocCounter { get; set; }
        ObservableCollection<EditorTabViewModel> Documents { get; }
        EditorTabViewModel? ActiveDocument { get; set; }
        EditorTabViewModel CreateNew();
        EditorTabViewModel? OpenFile(string? preselectedPath = null);
        bool Save(EditorTabViewModel doc);
        bool Close(EditorTabViewModel doc);
        //void Activate(EditorTabViewModel doc);
        //bool ConfirmClose(EditorTabViewModel doc);
    }

    public sealed partial class DocumentService : ObservableObject, IDocumentService
    {
        #region Services

        private readonly IEditorTabViewModelFactory mEditorFactory;
        private readonly IFileService mFileService;
        private readonly IDialogService mDialogService;
        private readonly OutputToolbox mOutputToolbox;
        private readonly IDockEventBinder mDockEvent;

        #endregion

        #region Var

        //private bool mIsRemovingFromCode;

        #endregion

        #region Fields

        public ObservableCollection<EditorTabViewModel> Documents { get; } = [];

        [ObservableProperty]
        public partial EditorTabViewModel? ActiveDocument { get; set; }

        public int NewDocCounter { get; set; }

        #endregion

        #region ~

        public DocumentService(IEditorTabViewModelFactory factory, IFileService fileService, IDialogService dialog, IDockEventBinder dockBinder, OutputToolbox output)
        {
            mEditorFactory = factory;
            mFileService = fileService;
            mDialogService = dialog;
            mDockEvent = dockBinder;
            mOutputToolbox = output;

            mDockEvent.ActiveContentChanged += OnActiveContentChanged;
            mDockEvent.DocumentClosing += OnDocumentClosing;
            //mDockEvent.DocumentClosed += OnDocumentClosed;
        }

        #endregion

        #region Public methods

        public EditorTabViewModel CreateNew()
        {
            NewDocCounter++;

            var doc = mEditorFactory.CreateNew($"Документ {NewDocCounter}", $"Содержимое документа №.\r\nСоздан: {DateTime.Now:G}");
            Documents.Add(doc);
            Activate(doc);
            mOutputToolbox.AppendLine($"Открыт документ: {doc.Title}");
            return doc;
        }

        public EditorTabViewModel? OpenFile(string? preselectedPath = null)
        {
            var path = preselectedPath ?? mFileService.ShowOpenDialog();
            if (path is null) 
                return null;

            // Если уже открыт — активируем
            var existing = Documents.FirstOrDefault(d => d.FilePath == path);

            if (existing != null)
            {
                Activate(existing);
                mOutputToolbox.AppendLine($"Активирован существующий: {path}");
                return existing;
            }

            try
            {
                var tab = mEditorFactory.CreateFromFile(path);
                Documents.Add(tab);
                Activate(tab);
                mOutputToolbox.AppendLine($"Открыт файл: {path}");
                return tab;
            }
            catch (Exception ex)
            {
                mDialogService.ShowInformation($"Не удалось открыть файл: {ex.Message}", "Ошибка");
                return null;
            }
        }

        public bool Save(EditorTabViewModel doc)
        {
            try
            {
                if (doc.SaveToFile(suggested => mFileService.ShowSaveDialog(suggested), mFileService.WriteAllText))
                {
                    mOutputToolbox.AppendLine($"Сохранён файл: {doc.FilePath}");
                    //понижаем счетчик новых документов
                    NewDocCounter--;
                    return true;
                }
            }
            catch (Exception ex)
            {
                mDialogService.ShowInformation($"Не удалось сохранить файл: {ex.Message}", "Ошибка");
            }

            return false;
        }

        public bool Close(EditorTabViewModel doc)
        {
            if (!ConfirmClose(doc))
                return false;

            Documents.Remove(doc);
            
            // если закрываемый файл сохранен на диске, счетчик открытых документов понижать не надо
            if(doc.FilePath == string.Empty)
                NewDocCounter--;
            
            mOutputToolbox.AppendLine($"Закрыт документ: {doc.Title}");

            if (Documents.Count == 0)
            {
                ActiveDocument = null;
                NewDocCounter = 0;
            }
                

            return true;
        }

        public bool ConfirmClose(EditorTabViewModel doc)
        {
            //закрывает без подтверждения если документ не изменен
            if (!doc.IsModified)
                return true;

            //окно сохранения если файл изменен
            var result = mDialogService.ShowQuestion($"Сохранить изменения в документе «{doc.Title}»?", "Несохранённые изменения");

            mOutputToolbox.AppendLine($"Закрытие «{doc.Title}»: выбор = {result}");

            if (result == MessageBoxResult.Yes)
                return Save(doc);

            return result != MessageBoxResult.Cancel;
        }

        #endregion

        #region Private methods

        private void Activate(EditorTabViewModel doc)
        {
            ActiveDocument = doc;
            doc.IsActive = true;
        }

        //закрывается
        private void OnDocumentClosing(object? sender, DocumentCancelEventArgs e)
        {
            if (e.Document.Content is not EditorTabViewModel doc)
                return;

            if (Close(doc))
                e.Cancel = true;
        }

        private void OnActiveContentChanged(object? sender, ActiveContentChangedEventArgs e)
        {
            if (e.NewContent is EditorTabViewModel doc)
                Activate(doc);
        }

        #endregion
    }
}
