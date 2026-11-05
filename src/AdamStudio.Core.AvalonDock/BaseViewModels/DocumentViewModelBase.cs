using CommunityToolkit.Mvvm.ComponentModel;

namespace AdamStudio.Core.AvalonDock.BaseViewModels
{
    /// <summary>
    /// Базовая виев модель для документов в понимании Avalon Dock
    /// </summary>
    public partial class DocumentViewModelBase : ObservableObject
    {
        [ObservableProperty]
        public partial string ContentId { get; set; } = $"doc-{Guid.NewGuid()}";
        private string title = string.Empty;

        /// <summary>Заголовок вкладки. Показывает «*», если есть несохранённые изменения.</summary>
        public string Title
        {
            get => IsModified ? $"{title} *" : title;
            set
            {
                if (SetProperty(ref title, value))
                    OnPropertyChanged(nameof(Title));
            }
        }


        // Хук генератора срабатывает ДО изменения свойства Content.
        // Чтобы избежать ложного срабатывания IsModified при первой загрузке файла, 
        // мы временно сбрасываем этот флаг в методе LoadFile ниже.
        partial void OnContentChanging(string value)
        {
            IsModified = true;
        }

        [ObservableProperty]
        public partial string ToolTip { get; set; } = string.Empty;

        [ObservableProperty]
        public partial bool IsActive { get; set; }

        // 2. При изменении IsModified генератор автоматически уведомит UI о том, что Title тоже изменился.
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Title))]
        public partial bool IsModified { get; set; }
    }
}
