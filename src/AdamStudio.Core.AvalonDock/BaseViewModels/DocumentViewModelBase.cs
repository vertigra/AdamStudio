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

        [ObservableProperty]
        public partial string ToolTip { get; set; } = string.Empty;

        [ObservableProperty]
        public partial bool IsActive { get; set; }

        /// <summary>
        /// При изменении IsModified = true в Title добавиться *
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Title))]
        public partial bool IsModified { get; set; }
    }
}
