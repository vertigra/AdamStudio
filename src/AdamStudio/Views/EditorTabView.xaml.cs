using System.Windows.Controls;

namespace AdamStudio.Views;

/// <summary>
/// View для вкладки документа. Биндится к EditorTabViewModel автоматически
/// через DataTemplate в App.xaml.
///
/// Code-behind намеренно пустой — вся логика во ViewModel (MVVM).
/// </summary>
public partial class EditorTabView : UserControl
{
    public EditorTabView() => InitializeComponent();
}
