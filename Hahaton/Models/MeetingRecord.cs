using System;
using System.Collections.Generic;

namespace MobileAssistant.Models
{
    public class MeetingRecord
    {
        /// <summary>
        /// Уникальный идентификатор записи встречи
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Название встречи
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Дата и время записи встречи
        /// </summary>
        public DateTime RecordedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// Путь к аудиофайлу
        /// </summary>
        public string AudioFilePath { get; set; }

        /// <summary>
        /// Длительность аудиозаписи в секундах
        /// </summary>
        public double Duration { get; set; }

        /// <summary>
        /// Результат транскрипции
        /// </summary>
        public TranscriptionResult Transcription { get; set; }

        /// <summary>
        /// Результат суммаризации
        /// </summary>
        public SummaryResult Summary { get; set; }

        /// <summary>
        /// Список выделенных обязательств
        /// </summary>
        public List<Commitment> Commitments { get; set; } = new List<Commitment>();

        /// <summary>
        /// Список участников встречи
        /// </summary>
        public List<SpeakerInfo> Speakers { get; set; } = new List<SpeakerInfo>();

        /// <summary>
        /// Статус обработки записи
        /// </summary>
        public MeetingProcessingStatus Status { get; set; } = MeetingProcessingStatus.New;

        /// <summary>
        /// Сообщение об ошибке (если есть)
        /// </summary>
        public string ErrorMessage { get; set; }
    }

    public enum MeetingProcessingStatus
    {
        New,               // Новая запись
        Transcribing,      // Идет процесс транскрибации
        Summarizing,       // Идет процесс суммаризации
        ExtractingCommitments, // Идет процесс выделения обязательств
        Completed,         // Обработка завершена
        Failed             // Ошибка обработки
    }
}