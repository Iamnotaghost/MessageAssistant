// Helpers/Converters/BoolToColorConverter.cs
using System.Globalization;

namespace MobileAssistant.Helpers.Converters
{
    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isTrue)
            {
                if (parameter is string colors)
                {
                    var parts = colors.Split(',');
                    if (parts.Length == 2)
                    {
                        return isTrue ? parts[0] : parts[1];
                    }
                }

                return isTrue ? Colors.Red : Colors.Green;
            }

            return Colors.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}