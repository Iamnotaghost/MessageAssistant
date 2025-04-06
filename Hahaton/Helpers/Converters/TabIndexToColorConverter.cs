// Helpers/Converters/TabIndexToColorConverter.cs
using System.Globalization;

namespace MobileAssistant.Helpers.Converters
{
    public class TabIndexToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int currentTabIndex && parameter is string paramStr && int.TryParse(paramStr, out int tabIndex))
            {
                return currentTabIndex == tabIndex ?
                    Color.FromRgba(0, 122, 255, 0.2) : // Выбранная вкладка
                    Colors.Transparent;                // Невыбранная вкладка
            }

            return Colors.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}