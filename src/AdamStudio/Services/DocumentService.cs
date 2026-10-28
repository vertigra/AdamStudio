using AdamStudio.Core.AvalonDock.EventArguments;
using AdamStudio.Core.AvalonDock.Services;
using AdamStudio.Core.Services;
using AdamStudio.Factories;
using AdamStudio.Model;
using AdamStudio.ViewModels;
using AvalonDock.Core.Events;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
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
    [Obsolete("Использует ViewModel нуждается в рефакторинге")]
    public interface IDocumentService : INotifyPropertyChanged
    {
        /// <summary>
        /// Переменная хранит номер документа и сколько всего вкладок открыто
        /// </summary>
        public int NewDocCounter { get; }
        ObservableCollection<EditorTabViewModel> Documents { get; }
        EditorTabViewModel? ActiveDocument { get; set; }
        EditorTabViewModel CreateNew();
        EditorTabViewModel? OpenFile(string? preselectedPath = null);
        EditorTabViewModel? LoadDocumentState(WorkspaceDocument savedDocument);
        void Activate(EditorTabViewModel doc);
        bool Save(EditorTabViewModel doc);
        bool Close(EditorTabViewModel doc);
    }

    [Obsolete("Использует ViewModel нуждается в рефакторинге")]
    public sealed partial class DocumentService : ObservableObject, IDocumentService
    {
        #region Services

        private readonly IEditorTabViewModelFactory mEditorFactory;
        private readonly IFileService mFileService;
        private readonly IDialogService mDialogService;
        private readonly IDockEventBinder mDockEvent;
        private readonly ILogService<DocumentService> mLogService;

        #endregion

        #region ~

        public DocumentService(IEditorTabViewModelFactory factory, IFileService fileService, IDialogService dialog, IDockEventBinder dockBinder, ILogService<DocumentService> logService)
        {
            mEditorFactory = factory;
            mFileService = fileService;
            mDialogService = dialog;
            mDockEvent = dockBinder;
            mLogService = logService;

            mDockEvent.ActiveContentChanged += OnActiveContentChanged;
            mDockEvent.DocumentClosing += OnDocumentClosing;

            Documents = [];
            Documents.CollectionChanged += DocumentsCollectionChanged;
        }

        #endregion

        private void DocumentsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
                NewDocCounter++;

            if (e.Action == NotifyCollectionChangedAction.Remove)
                NewDocCounter--;
        }

        #region Fields

        public ObservableCollection<EditorTabViewModel> Documents { get;  } 

        [ObservableProperty]
        public partial EditorTabViewModel? ActiveDocument { get; set; }

        public int NewDocCounter { get; private set; }

        #endregion

        #region Public methods

        public EditorTabViewModel CreateNew()
        {
            var doc = mEditorFactory.CreateNew($"Документ {NewDocCounter}", $"Содержимое документа №.\r\nСоздан: {DateTime.Now:G}");
            Documents.Add(doc);
            Activate(doc);
            mLogService.LogInformation($"Открыт документ: {doc.Title}");
            return doc;
        }

        public EditorTabViewModel LoadDocumentState(WorkspaceDocument savedDocument)
        {
            var doc = mEditorFactory.LoadDocumentState(savedDocument);
            Documents.Add(doc);
            
            mLogService.LogInformation($"Восстановлен документ: {doc.Title}");
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
                mLogService.LogInformation($"Активирован существующий: {path}");
                return existing;
            }

            try
            {
                var tab = mEditorFactory.CreateFromFile(path);
                Documents.Add(tab);
                Activate(tab);
                mLogService.LogInformation($"Открыт файл: {path}");
                return tab;
            }
            catch (Exception ex)
            {
                mLogService.LogError("Не удалось открыть файл", ex);
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
                    mLogService.LogInformation($"Сохранён файл: {doc.FilePath}");
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
            
            mLogService.LogInformation($"Закрыт документ: {doc.Title}");

            if (Documents.Count == 0)
            {
                ActiveDocument = null;
            }
                

            return true;
        }

        public bool ConfirmClose(EditorTabViewModel doc)
        {
            //закрывает без подтверждения если документ не изменен
            if (!doc.IsModified)
            {
                //NewDocCounter--;
                return true;
            }
                

            //окно сохранения если файл изменен
            var result = mDialogService.ShowQuestion($"Сохранить изменения в документе «{doc.Title}»?", "Несохранённые изменения");

            mLogService.LogInformation($"Закрытие «{doc.Title}»: выбор = {result}");

            if (result == MessageBoxResult.Yes)
                return Save(doc);

            return result != MessageBoxResult.Cancel;
        }

        #endregion

        #region Private methods

        public void Activate(EditorTabViewModel doc)
        {
            ActiveDocument = doc;
            doc.IsActive = true;
        }

        //закрывается
        private void OnDocumentClosing(object? sender, DocumentCancelEventArgs e)
        {
            if (e.Document.Content is not EditorTabViewModel doc)
                return;

            // Если пользователь отказался закрывать (нажал Отмена в диалоге)
            if (!ConfirmClose(doc))
            {
                e.Cancel = true; // Отменяем закрытие в AvalonDock!
                return;
            }


            // Если согласился - ничего не делаем. AvalonDock сам закроет вкладку,
            // и это автоматически уберет её из DocumentsSource (коллекции Documents).

            //автоматически не убирает, нужно так
            Documents.Remove(doc);
        }

        private void OnActiveContentChanged(object? sender, ActiveContentChangedEventArgs e)
        {
            if (e.NewContent is EditorTabViewModel doc)
                Activate(doc);
        }

        #endregion
    }
}
