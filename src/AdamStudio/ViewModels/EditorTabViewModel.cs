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
    private readonly ILogService<EditorTabViewModel> mLogService;

    public EditorTabViewModel(IFileService fileService, IDialogService dialogService, ILogService<EditorTabViewModel> logService)
    {
        mFileService = fileService;
        mDialogService = dialogService;
        mLogService = logService;
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
            mLogService.LogError("Ошибка чтения файла", ex);
            mDialogService.ShowInformation($"Ошибка чтения файла: {ex.Message}", "Ошибка");
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
            mLogService.LogError("Не удалось сохранить", ex);
            mDialogService.ShowInformation($"Не удалось сохранить: {ex.Message}", "Ошибка");
            return false;
        }
    }
}
