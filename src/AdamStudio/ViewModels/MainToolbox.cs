using AdamStudio.Services;
using AvalonDock.Layout;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace AdamStudio.ViewModels
{
    public sealed partial class MainToolbox : ToolViewModelBase
    {
        #region Services
        private readonly IDocumentService mDocumentService;

        #endregion

        public MainToolbox(IDocumentService documentService): base("Панель инcтрументов", "MainTool")
        {
            PreferredSide = AnchorSide.Left;

            mDocumentService = documentService;

            mDocumentService.PropertyChanged += OnDocumentServicePropertyChanged;
        }

        private void OnDocumentServicePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IDocumentService.ActiveDocument))
            {
                OnPropertyChanged(nameof(ActiveDocument));

                SaveFileCommand.NotifyCanExecuteChanged();
                CloseDocumentCommand.NotifyCanExecuteChanged();
            }
        }

        #region Fields

        public ObservableCollection<EditorTabViewModel> Documents => mDocumentService.Documents;
        public EditorTabViewModel? ActiveDocument => mDocumentService.ActiveDocument;

        #endregion

        #region Command

        [RelayCommand]
        private void OpenNewDocument() => mDocumentService.CreateNew();

        [RelayCommand]
        private void OpenFile() => mDocumentService.OpenFile();

        [RelayCommand(CanExecute = nameof(CanSaveOrClose))]
        private void SaveFile()
        {
            if (ActiveDocument is { } doc)
                mDocumentService.Save(doc);
        }

        [RelayCommand(CanExecute = nameof(CanSaveOrClose))]
        private void CloseDocument()
        {
            if (ActiveDocument is { } doc)
                mDocumentService.Close(doc);
        }

        private bool CanSaveOrClose() => ActiveDocument is not null;

        #endregion
    }
}
