// Helpers/Converters/StatusToColorConverter.cs
using MobileAssistant.Models;
using System.Globalization;

namespace MobileAssistant.Helpers.Converters
{
    public class StatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is MeetingProcessingStatus status)
            {
                return status switch
                {
                    MeetingProcessingStatus.New => Colors.Blue,
                    MeetingProcessingStatus.Transcribing => Colors.Orange,
                    MeetingProcessingStatus.Summarizing => Colors.Orange,
                    MeetingProcessingStatus.ExtractingCommitments => Colors.Orange,
                    MeetingProcessingStatus.Completed => Colors.Green,
                    MeetingProcessingStatus.Failed => Colors.Red,
                    _ => Colors.Gray
                };
            }

            return Colors.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}