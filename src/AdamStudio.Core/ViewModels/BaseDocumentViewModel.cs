using AdamStudio.Core.AvalonDock.ViewModels;
using AdamStudio.Core.Services;
using System.Windows;

namespace AdamStudio.Core.ViewModels
{
    public class BaseDocumentViewModel : AvalonDocumentViewModel
    {
        private readonly IDialogService mDialogService;

        public BaseDocumentViewModel(IDialogService dialogService) 
        {
            mDialogService = dialogService;
        }


        public override bool ConfirmClose()
        {
            if (!IsModified)
                return true;

            var result = mDialogService.ShowQuestion($"Сохранить изменения в «{Title}»?", "Несохранённые изменения");

            if (result == MessageBoxResult.Yes)
                return SaveToFile();

            return result != MessageBoxResult.Cancel;
        }
    }
}
