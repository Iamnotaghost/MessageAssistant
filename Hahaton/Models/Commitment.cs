using System;

namespace MobileAssistant.Models
{
    public class Commitment
    {
        /// <summary>
        /// Уникальный идентификатор обязательства
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Текст обязательства
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Ответственное лицо (имя или идентификатор говорящего)
        /// </summary>
        public string ResponsiblePerson { get; set; }

        /// <summary>
        /// Срок исполнения
        /// </summary>
        public DateTime? Deadline { get; set; }

        /// <summary>
        /// Статус выполнения обязательства
        /// </summary>
        public CommitmentStatus Status { get; set; } = CommitmentStatus.Pending;

        /// <summary>
        /// Сегмент транскрипции, из которого выделено обязательство
        /// </summary>
        public int SourceSegmentIndex { get; set; }

        /// <summary>
        /// Идентификатор транскрипции, из которой выделено обязательство
        /// </summary>
        public string TranscriptionId { get; set; }

        /// <summary>
        /// Дата и время создания обязательства
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public enum CommitmentStatus
    {
        Pending,   // Ожидает выполнения
        InProgress,// В процессе выполнения
        Completed, // Выполнено
        Postponed, // Отложено
        Cancelled  // Отменено
    }
}