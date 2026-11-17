using AdamStudio.Core.AvalonDock.Services;
using AdamStudio.Core.Services;
using AdamStudio.Core.ViewModels;
using AdamStudio.Messages;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.ComponentModel;

namespace AdamStudio.ViewModels
{
    public class SettingsViewModel : BaseDocumentViewModel, IRecipient<OpenSettingsMessage>
    {
        public ISettingsService SettingsService { get; }

        private readonly IDockWindowService mDockWindowService;

        private readonly ILogService<SettingsViewModel> mLogService;

        public SettingsViewModel(ISettingsService settingsService, IDockWindowService dockWindowService, IMessenger messenger, ILogService<SettingsViewModel> logService, IDialogService dialogService) : base(dialogService)
        {
            SettingsService = settingsService;
            mDockWindowService = dockWindowService;
            mLogService = logService;

            Title = "Настройки";
            ContentId = nameof(SettingsViewModel);
            ToolTip = "Настройки приложения";

            messenger.RegisterAll(this);

            SettingsService.Current.PropertyChanged += SettingsChanged;
        }

        private void SettingsChanged(object? sender, PropertyChangedEventArgs e)
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

        /// <summary>
        /// Настройки сохраняются автоматически через ISettingsService при изменении свойств.
        /// Команда "Сохранить" в MainViewModel просто вызовет этот метод, который сбросит IsModified.
        /// </summary>
        public override bool SaveToFile()
        {
            try
            {
                SettingsService.Save(); 
                IsModified = false;
            }
            catch(Exception ex)
            {
                mLogService.LogError("Ошибка при сохранении настроек", ex);
            }
            
            return true;
        }
    }
}
