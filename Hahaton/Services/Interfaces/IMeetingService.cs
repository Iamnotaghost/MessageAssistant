using MobileAssistant.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MobileAssistant.Services.Interfaces
{
    public interface IMeetingService
    {
        /// <summary>
        /// Создать новую запись встречи
        /// </summary>
        Task<MeetingRecord> CreateMeetingRecordAsync(string title, string audioFilePath);

        /// <summary>
        /// Получить запись встречи по ID
        /// </summary>
        Task<MeetingRecord> GetMeetingRecordAsync(string id);

        /// <summary>
        /// Получить все записи встреч
        /// </summary>
        Task<List<MeetingRecord>> GetAllMeetingRecordsAsync();

        /// <summary>
        /// Обновить запись встречи
        /// </summary>
        Task UpdateMeetingRecordAsync(MeetingRecord meetingRecord);

        /// <summary>
        /// Удалить запись встречи
        /// </summary>
        Task DeleteMeetingRecordAsync(string id);

        /// <summary>
        /// Обработать аудиозапись встречи (транскрипция + суммаризация + выделение обязательств)
        /// </summary>
        Task<MeetingRecord> ProcessMeetingAudioAsync(string meetingId);

        /// <summary>
        /// Обновить имена говорящих для встречи
        /// </summary>
        Task UpdateSpeakerNamesAsync(string meetingId, Dictionary<int, string> speakerNames);

        /// <summary>
        /// Событие изменения статуса обработки
        /// </summary>
        event EventHandler<(string MeetingId, MeetingProcessingStatus Status, int Progress)> ProcessingStatusChanged;
    }
}