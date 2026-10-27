using System.Windows;

namespace AdamStudio.Core.Services;

/// <summary>
/// Абстракция сервиса диалогов — чтобы ViewModel не зависел от View напрямую.
/// </summary>
public interface IDialogService
{
    MessageBoxResult ShowQuestion(string message, string title);
    void ShowInformation(string message, string title);
}

public sealed class DialogService : IDialogService
{
    public MessageBoxResult ShowQuestion(string message, string title) => MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);

    public void ShowInformation(string message, string title) => MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
}
