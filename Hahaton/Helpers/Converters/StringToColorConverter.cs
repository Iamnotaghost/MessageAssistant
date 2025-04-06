// Helpers/Converters/StringToColorConverter.cs
using System.Globalization;

namespace MobileAssistant.Helpers.Converters
{
    public class StringToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string hexColor && !string.IsNullOrEmpty(hexColor))
            {
                try
                {
                    return Color.FromArgb(hexColor);
                }
                catch
                {
                    return Colors.Gray;
                }
            }

            return Colors.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}