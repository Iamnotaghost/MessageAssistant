// Models/TranscriptionResult.cs
using System;
using System.Collections.Generic;

namespace MobileAssistant.Models
{
    public class TranscriptionResult
    {
        /// <summary>
        /// Уникальный идентификатор транскрипции
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Сегменты транскрипции с информацией о говорящих
        /// </summary>
        public List<Segment> Segments { get; set; } = new List<Segment>();

        /// <summary>
        /// Полный текст транскрипции
        /// </summary>
        public string FullText { get; set; }

        /// <summary>
        /// Длительность аудио в секундах
        /// </summary>
        public double Duration { get; set; }

        /// <summary>
        /// Дата и время выполнения транскрипции
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}