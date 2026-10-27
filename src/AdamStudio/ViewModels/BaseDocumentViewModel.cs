using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace AdamStudio.ViewModels
{
    [Obsolete("Пока не используется")]
    public partial class BaseDocumentViewModel : ObservableObject
    {
        [ObservableProperty]
        public partial string ContentId { get; set; } = $"doc-{Guid.NewGuid()}";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Title))]
        public partial string DocumentName { get; set; } = "Без имени";

        public string Title
        {
            get => IsModified ? $"{DocumentName} *" : DocumentName;
            set => DocumentName = value;
        }

        [ObservableProperty]
        public partial string ToolTip { get; set; } = "";

        [ObservableProperty]
        public partial bool IsActive { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Title))]
        public partial bool IsModified { get; set; }

        /// <summary>Сохранить документ. Возвращает false при отмене.</summary>
        public virtual bool SaveToFile() => false;

        /// <summary>Подтвердить закрытие. Возвращает false, чтобы отменить.</summary>
        public virtual bool ConfirmClose() => true;
    }
}
