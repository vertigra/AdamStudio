using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.IO;

namespace AdamStudio.ViewModels;

/// <summary>
/// ViewModel вкладки документа. В классическом DockingManager (с DocumentsSource)
/// это просто VM с INotifyPropertyChanged + свойства Title/Content/IsModified.
/// </summary>
public sealed partial class EditorTabViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string ContentId { get; set; } = $"doc-{Guid.NewGuid()}";

    // 1. Создаем автосвойство для "чистого" имени документа.
    // Атрибут [NotifyPropertyChangedFor] заставит UI обновлять Title при каждом изменении DocumentName.
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Title))]
    public partial string DocumentName { get; set; } =  "Без имени";

    /// <summary>Заголовок вкладки. Показывает «*», если есть несохранённые изменения.</summary>
    public string Title
    {
        get => IsModified ? $"{DocumentName} *" : DocumentName;
        set => DocumentName = value; // Просто пробрасываем значение в генерируемое свойство
    }

    [ObservableProperty]
    public partial string FilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Content { get; set; } = string.Empty;

    // Хук генератора срабатывает ДО изменения свойства Content.
    // Чтобы избежать ложного срабатывания IsModified при первой загрузке файла, 
    // мы временно сбрасываем этот флаг в методе LoadFile ниже.
    partial void OnContentChanging(string value)
    {
        IsModified = true;
    }

    [ObservableProperty]
    public partial string ToolTip { get; set; } = "Новый документ";

    [ObservableProperty]
    public partial bool IsActive { get; set; } 

    // 2. При изменении IsModified генератор автоматически уведомит UI о том, что Title тоже изменился.
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Title))]
    public partial bool IsModified { get; set; }

    /// <summary>Загрузить содержимое файла в документ.</summary>
    public void LoadFile(string path)
    {
        FilePath = path;
        Title = Path.GetFileName(path);
        ToolTip = path;
        ContentId = path;

        try
        {
            Content = File.ReadAllText(path);
            // OnContentChanging взведет флаг IsModified в true, 
            // поэтому сбрасываем его обратно в false ПОСЛЕ присвоения Content.
            IsModified = false;
        }
        catch (Exception ex)
        {
            Content = $"Ошибка чтения файла: {ex.Message}";
        }
    }

    /// <summary>Сохранить содержимое в файл. Возвращает false при отмене.</summary>
    public bool SaveToFile(Func<string?, string?> showSaveDialog, Action<string, string> writeAllText)
    {
        var path = FilePath;

        if (string.IsNullOrEmpty(path))
        {
            // Здесь передаем чистое имя без звездочки для диалога сохранения
            path = showSaveDialog(DocumentName);

            if (path is null)
                return false;
        }

        writeAllText(path, Content);
        FilePath = path;
        Title = Path.GetFileName(path);
        IsModified = false;
        return true;
    }
}
