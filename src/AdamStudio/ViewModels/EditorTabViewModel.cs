using AdamStudio.Core.AvalonDock.BaseViewModels;
using AdamStudio.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.IO;
using System.Windows;

namespace AdamStudio.ViewModels;

/// <summary>
/// ViewModel вкладки документа. В классическом DockingManager (с DocumentsSource)
/// это просто VM с INotifyPropertyChanged + свойства Title/Content/IsModified.
/// </summary>
public sealed partial class EditorTabViewModel : DocumentViewModelBase
{
    private readonly IFileService mFileService;
    private readonly IDialogService mDialogService;
    public EditorTabViewModel(IFileService fileService, IDialogService dialogService)
    {
        mFileService = fileService;
        mDialogService = dialogService;
    }

    [ObservableProperty]
    public partial string Content { get; set; } = string.Empty;

    // Хук генератора срабатывает ДО изменения свойства Content.
    // Чтобы избежать ложного срабатывания IsModified при первой загрузке файла, 
    // мы временно сбрасываем этот флаг в методе LoadFile ниже.
    partial void OnContentChanging(string value) => IsModified = true;

    [ObservableProperty]
    public partial string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Загрузить содержимое файла в документ.
    /// </summary>
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

    public override bool ConfirmClose()
    {
        if (!IsModified) 
            return true;

        var result = mDialogService.ShowQuestion($"Сохранить изменения в «{Title}»?", "Несохранённые изменения");
        
        if (result == MessageBoxResult.Yes) 
            return SaveToFile();

        return result != MessageBoxResult.Cancel;
    }

    public override bool SaveToFile()
    {
        var path = FilePath;
        if (string.IsNullOrEmpty(path))
        {
            path = mFileService.ShowSaveDialog(Title);
            if (path is null) 
                return false;
        }

        try
        {
            mFileService.WriteAllText(path, Content);
            FilePath = path;
            Title = Path.GetFileName(path);
            IsModified = false;
            return true;
        }
        catch (Exception ex)
        {
            mDialogService.ShowInformation($"Не удалось сохранить: {ex.Message}", "Ошибка");
            return false;
        }
    }

    /// <summary>Сохранить содержимое в файл. Возвращает false при отмене.</summary>
    /*public bool SaveToFile(Func<string?, string?> showSaveDialog, Action<string, string> writeAllText)
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
    }*/
}
