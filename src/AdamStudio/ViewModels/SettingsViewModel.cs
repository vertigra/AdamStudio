using AdamStudio.Core.AvalonDock.BaseViewModels;
using AdamStudio.Core.Services;

namespace AdamStudio.ViewModels
{
    public class SettingsViewModel : DocumentViewModelBase
    {
        public ISettingsService SettingsService { get; }

        public SettingsViewModel(ISettingsService settingsService)
        {
            SettingsService = settingsService;

            Title = "Настройки";
            ContentId = nameof(SettingsViewModel); 
            ToolTip = "Настройки приложения";
        }


        // Настройки сохраняются автоматически через ISettingsService при изменении свойств.
        // Команда "Сохранить" в MainViewModel просто вызовет этот метод, который сбросит IsModified.
        public override bool SaveToFile()
        {
            IsModified = false;
            return true;
        }
    }
}
