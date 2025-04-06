// Helpers/Converters/SpeakerIdToColorConverter.cs
using MobileAssistant.Models;
using System.Globalization;

namespace MobileAssistant.Helpers.Converters
{
    public class SpeakerIdToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int speakerId && parameter is List<SpeakerInfo> speakers)
            {
                var speaker = speakers.FirstOrDefault(s => s.Id == speakerId);
                if (speaker != null && !string.IsNullOrEmpty(speaker.ColorHex))
                {
                    try
                    {
                        return Color.FromArgb(speaker.ColorHex).WithAlpha(0.2f); // Прозрачная версия для фона
                    }
                    catch
                    {
                        return Colors.Transparent;
                    }
                }
            }

            return Colors.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}