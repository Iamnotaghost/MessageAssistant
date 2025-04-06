// Helpers/Converters/StatusToStringConverter.cs
using MobileAssistant.Models;
using System.Globalization;

namespace MobileAssistant.Helpers.Converters
{
    public class StatusToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is MeetingProcessingStatus status)
            {
                return status switch
                {
                    MeetingProcessingStatus.New => "Новая запись",
                    MeetingProcessingStatus.Transcribing => "Идет распознавание речи",
                    MeetingProcessingStatus.Summarizing => "Создание краткого пересказа",
                    MeetingProcessingStatus.ExtractingCommitments => "Выделение обязательств",
                    MeetingProcessingStatus.Completed => "Обработка завершена",
                    MeetingProcessingStatus.Failed => "Ошибка обработки",
                    _ => "Неизвестный статус"
                };
            }

            return "Неизвестный статус";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}