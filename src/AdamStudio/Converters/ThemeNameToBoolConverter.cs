using System;
using System.Globalization;
using System.Windows.Data;

namespace AdamStudio.Converters
{
    /// <summary>
    /// Конвертер для отметки галочкой текущей темы в меню.
    /// Сравнивает имя темы из DataContext (Menu Item) с CurrentThemeName из MainViewModel.
    ///
    /// converterParameter не используется — сравнение идёт по DataContext кнопки (ThemeInfo.Name)
    /// с value (CurrentThemeName).
    /// </summary>
    public sealed class ThemeNameToBoolConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // values[0] = CurrentThemeName (из MainViewModel)
            // values[1] = Name (из ThemeInfo — DataContext MenuItem)
            if (values.Length < 2) return false;
            var current = values[0] as string;
            var candidate = values[1] as string;
            return string.Equals(current, candidate, StringComparison.OrdinalIgnoreCase);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();

    }
}
