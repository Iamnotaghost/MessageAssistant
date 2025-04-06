using MobileAssistant.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MobileAssistant.Services.Interfaces
{
    public interface ITextProcessingService
    {
        /// <summary>
        /// Создать краткий пересказ транскрипции
        /// </summary>
        Task<SummaryResult> SummarizeTranscriptionAsync(TranscriptionResult transcription);

        /// <summary>
        /// Выделить обязательства из транскрипции
        /// </summary>
        Task<List<Commitment>> ExtractCommitmentsAsync(TranscriptionResult transcription);

        /// <summary>
        /// Идентифицировать говорящих по транскрипции
        /// </summary>
        Task<List<SpeakerInfo>> IdentifySpeakersAsync(TranscriptionResult transcription);

        /// <summary>
        /// Отменить текущую операцию обработки текста
        /// </summary>
        Task CancelProcessingAsync();
    }
}