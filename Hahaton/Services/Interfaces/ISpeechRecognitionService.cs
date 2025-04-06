using MobileAssistant.Models;
using System.Threading.Tasks;

namespace MobileAssistant.Services.Interfaces
{
    public interface ISpeechRecognitionService
    {
        /// <summary>
        /// Распознать речь из аудиофайла
        /// </summary>
        Task<TranscriptionResult> TranscribeAudioAsync(string audioFilePath);
        /// <summary>
        /// Распознать речь из аудиофайла с разделением по говорящим
        /// </summary>
        Task<TranscriptionResult> TranscribeAudioWithDiarizationAsync(string audioFilePath);
        /// <summary>
        /// Отменить текущую операцию распознавания
        /// </summary>
        Task CancelTranscriptionAsync();
        /// <summary>
        /// Проверить статус транскрипции по ID
        /// </summary>
        Task<ApiResponse<TranscriptionResult>> CheckTranscriptionStatusAsync(string transcriptionId);
        /// <summary>
        /// Событие прогресса распознавания
        /// </summary>
        event EventHandler<int> TranscriptionProgressChanged;
    }
}