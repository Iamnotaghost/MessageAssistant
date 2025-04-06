// Models/Segment.cs
namespace MobileAssistant.Models
{
    public class Segment
    {
        /// <summary>
        /// Текст сегмента транскрипции
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Идентификатор говорящего
        /// </summary>
        public int SpeakerId { get; set; }

        /// <summary>
        /// Имя говорящего (если задано пользователем)
        /// </summary>
        public string SpeakerName { get; set; }

        /// <summary>
        /// Время начала сегмента в секундах от начала записи
        /// </summary>
        public double StartTime { get; set; }

        /// <summary>
        /// Время окончания сегмента в секундах
        /// </summary>
        public double EndTime { get; set; }

        /// <summary>
        /// Уверенность в распознавании (от 0 до 1)
        /// </summary>
        public float Confidence { get; set; }
    }
}