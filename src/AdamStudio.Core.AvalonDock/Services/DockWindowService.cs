using AdamStudio.Core.AvalonDock.BaseViewModels;
using AdamStudio.Core.AvalonDock.EventArguments;
using AvalonDock.Core.Events;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace AdamStudio.Core.AvalonDock.Services
{
    public interface IDockWindowService : INotifyPropertyChanged
    {
        ObservableCollection<DocumentViewModelBase> Documents { get; }
        DocumentViewModelBase? ActiveDocument { get;  }

        void OpenDocument(DocumentViewModelBase doc);
        void ActivateDocument(DocumentViewModelBase doc);
        bool CloseDocument(DocumentViewModelBase doc);
    }

    public sealed partial class DockWindowService : ObservableObject, IDockWindowService
    {
        private readonly IDockEventBinder mDockEventBinder;

        public DockWindowService(IDockEventBinder dockEventBinder)
        {
            mDockEventBinder = dockEventBinder;
            mDockEventBinder.ActiveContentChanged += OnActiveContentChanged;
            mDockEventBinder.DocumentClosing += OnDocumentClosing;
        }

        public ObservableCollection<DocumentViewModelBase> Documents { get; } = [];

        [ObservableProperty]
        public partial DocumentViewModelBase? ActiveDocument { get; private set; }

        public void OpenDocument(DocumentViewModelBase doc)
        {
            if (Documents.Contains(doc))
            {
                ActivateDocument(doc);
                return;
            }

            Documents.Add(doc);
        }

        public void ActivateDocument(DocumentViewModelBase doc)
        {
            ActiveDocument = doc;
            doc.IsActive = true;
        }

        public bool CloseDocument(DocumentViewModelBase doc)
        {
            if (!doc.ConfirmClose())
                return false;

            Documents.Remove(doc);
            return true;
        }

        private void OnActiveContentChanged(object? sender, ActiveContentChangedEventArgs e)
        {
            if (e.NewContent is DocumentViewModelBase doc)
                ActivateDocument(doc);
        }

        private void OnDocumentClosing(object? sender, DocumentCancelEventArgs e)
        {
            if (e.Document.Content is not DocumentViewModelBase doc) 
                return;

            if (!doc.ConfirmClose())
            {
                e.Cancel = true; // Отменяем закрытие в AvalonDock
                return;
            }

            // Если согласился - AvalonDock сам закроет, но нужно удалить из коллекции
            Documents.Remove(doc);
        }
    }
}
