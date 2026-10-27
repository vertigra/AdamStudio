using System.IO;
using Microsoft.Win32;

namespace AdamStudio.Core.Services;

/// <summary>
/// Абстракция над файловыми диалогами WPF.
/// Позволяет ViewModel-и работать с файлами, не ссылаясь на Microsoft.Win32.
/// </summary>
public interface IFileService
{
    string? ShowOpenDialog(string title = "Открыть документ", string filter = "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*");
    string? ShowSaveDialog(string? suggestedFileName = null, string filter = "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*");
    string ReadAllText(string path);
    void WriteAllText(string path, string content);   // ← (string, string) → void
}

public sealed class FileService //: IFileService
{
    public string? ShowOpenDialog(string title = "Открыть документ", string filter = "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*")
    {
        var dlg = new OpenFileDialog
        {
            Title = title,
            Filter = filter,
            CheckFileExists = true,
            Multiselect = false
        };
        return dlg.ShowDialog() == true ? dlg.FileName : null;
    }

    public string? ShowSaveDialog(string? suggestedFileName = null, string filter = "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*")
    {
        var dlg = new SaveFileDialog
        {
            Title = "Сохранить документ",
            Filter = filter,
            FileName = suggestedFileName ?? "Документ.txt",
            OverwritePrompt = true,
            AddExtension = true
        };
        return dlg.ShowDialog() == true ? dlg.FileName : null;
    }

    public string ReadAllText(string path) => File.ReadAllText(path);

    public void WriteAllText(string path, string content)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        File.WriteAllText(path, content);
    }
}
