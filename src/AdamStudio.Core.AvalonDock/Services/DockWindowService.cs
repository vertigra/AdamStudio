using AdamStudio.Core.AvalonDock.ViewModels;
using AdamStudio.Core.AvalonDock.EventArguments;
using AvalonDock.Core.Events;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace AdamStudio.Core.AvalonDock.Services
{
    public interface IDockWindowService : INotifyPropertyChanged
    {
        ObservableCollection<AvalonDocumentViewModel> Documents { get; }
        AvalonDocumentViewModel? ActiveDocument { get;  }

        void OpenDocument(AvalonDocumentViewModel doc);
        void ActivateDocument(AvalonDocumentViewModel doc);
        bool CloseDocument(AvalonDocumentViewModel doc);
    }

    public sealed partial class DockWindowService : ObservableObject, IDockWindowService
    {
        //private readonly IDockEventBinder mDockEventBinder;
        private readonly ILogger<DockWindowService> mLogger;

        public DockWindowService(IDockEventBinder dockEventBinder, ILogger<DockWindowService> logger)
        {
            mLogger = logger;

            dockEventBinder.ActiveContentChanged += OnActiveContentChanged;
            dockEventBinder.DocumentClosing += OnDocumentClosing;
        }

        public ObservableCollection<AvalonDocumentViewModel> Documents { get; } = [];

        [ObservableProperty]
        public partial AvalonDocumentViewModel? ActiveDocument { get; private set; }

        partial void OnActiveDocumentChanged(AvalonDocumentViewModel? value)
        {
            mLogger.LogTrace($"Активный документ {value?.Title ?? "Title не определен"}");
        }

        public void OpenDocument(AvalonDocumentViewModel doc)
        {
            if (Documents.Contains(doc))
            {
                ActivateDocument(doc);
                return;
            }

            Documents.Add(doc);
        }

        public void ActivateDocument(AvalonDocumentViewModel doc)
        {
            ActiveDocument = doc;
            doc.IsActive = true;
        }

        public bool CloseDocument(AvalonDocumentViewModel doc)
        {
            if (!doc.ConfirmClose())
                return false;

            Documents.Remove(doc);
            return true;
        }

        private void OnActiveContentChanged(object? sender, ActiveContentChangedEventArgs e)
        {
            if (e.NewContent is AvalonDocumentViewModel doc)
                ActivateDocument(doc);
        }

        private void OnDocumentClosing(object? sender, DocumentCancelEventArgs e)
        {
            if (e.Document.Content is not AvalonDocumentViewModel doc) 
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
