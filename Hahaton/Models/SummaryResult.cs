using System;

namespace MobileAssistant.Models
{
    public class SummaryResult
    {
        /// <summary>
        /// Уникальный идентификатор результата суммаризации
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Краткий пересказ встречи
        /// </summary>
        public string Summary { get; set; }

        /// <summary>
        /// Ключевые темы, обсуждавшиеся на встрече
        /// </summary>
        public List<string> KeyTopics { get; set; } = new List<string>();

        /// <summary>
        /// Идентификатор транскрипции, для которой сделана суммаризация
        /// </summary>
        public string TranscriptionId { get; set; }

        /// <summary>
        /// Дата и время создания суммаризации
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}