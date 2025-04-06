using MobileAssistant.Models;
using System.Threading.Tasks;

namespace MobileAssistant.Services.Interfaces
{
    public interface IEmailService
    {
        /// <summary>
        /// Отправить результаты обработки встречи на почту
        /// </summary>
        Task<bool> SendMeetingResultsAsync(MeetingRecord meetingRecord, EmailSettings settings);

        /// <summary>
        /// Сформировать текстовое содержимое письма с результатами встречи
        /// </summary>
        Task<string> GenerateEmailContentAsync(MeetingRecord meetingRecord, EmailSettings settings);
    }
}