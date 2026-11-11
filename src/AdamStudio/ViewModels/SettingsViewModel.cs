using AdamStudio.Core.AvalonDock.BaseViewModels;
using AdamStudio.Core.AvalonDock.Services;
using AdamStudio.Core.Services;
using AdamStudio.Extensions;
using CommunityToolkit.Mvvm.Messaging;

namespace AdamStudio.ViewModels
{
    public class SettingsViewModel : DocumentViewModelBase, IRecipient<OpenSettingsMessage>
    {
        public ISettingsService SettingsService { get; }

        private readonly IDockWindowService mDockWindowService;

        public SettingsViewModel(ISettingsService settingsService, IDockWindowService dockWindowService, IMessenger messenger)
        {
            SettingsService = settingsService;
            mDockWindowService = dockWindowService;

            Title = "Настройки";
            ContentId = nameof(SettingsViewModel); 
            ToolTip = "Настройки приложения";

            messenger.RegisterAll(this);

            SettingsService.Current.PropertyChanged += SettingsChanged;
        }

        private void SettingsChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            IsModified = true;
        }

        public void Receive(OpenSettingsMessage message)
        {
            if (mDockWindowService.Documents.Contains(this))
                mDockWindowService.ActivateDocument(this);
            else
            {
                mDockWindowService.OpenDocument(this);
                mDockWindowService.ActivateDocument(this);
            }
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
