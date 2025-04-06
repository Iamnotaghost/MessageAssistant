using System.Threading.Tasks;

namespace MobileAssistant.Services.Interfaces
{
    public interface IAudioService
    {
        /// <summary>
        /// Начать запись аудио
        /// </summary>
        Task StartRecordingAsync();

        /// <summary>
        /// Остановить запись и вернуть путь к файлу
        /// </summary>
        Task<string> StopRecordingAsync();

        /// <summary>
        /// Выбрать аудиофайл из хранилища устройства
        /// </summary>
        Task<string> PickAudioFileAsync();

        /// <summary>
        /// Получить длительность аудиофайла в секундах
        /// </summary>
        Task<double> GetAudioDurationAsync(string filePath);

        /// <summary>
        /// Проверить, идет ли запись
        /// </summary>
        bool IsRecording { get; }

        /// <summary>
        /// Текущая длительность записи в секундах
        /// </summary>
        double CurrentRecordingDuration { get; }

        /// <summary>
        /// Событие изменения длительности записи
        /// </summary>
        event EventHandler<double> RecordingDurationChanged;
    }
}