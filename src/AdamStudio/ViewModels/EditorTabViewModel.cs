using AdamStudio.Core.AvalonDock.BaseViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.IO;

namespace AdamStudio.ViewModels;

/// <summary>
/// ViewModel вкладки документа. В классическом DockingManager (с DocumentsSource)
/// это просто VM с INotifyPropertyChanged + свойства Title/Content/IsModified.
/// </summary>
public sealed partial class EditorTabViewModel : DocumentViewModelBase
{
    [ObservableProperty]
    public partial string Content { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string FilePath { get; set; } = string.Empty;

    /// <summary>Загрузить содержимое файла в документ.</summary>
    public  void LoadFile(string path)
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
            path = showSaveDialog(Title);

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
