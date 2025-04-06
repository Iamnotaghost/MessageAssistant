// Services/Implementation/EmailService.cs
using MobileAssistant.Models;
using MobileAssistant.Services.Interfaces;
using System;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace MobileAssistant.Services.Implementation
{
    public class EmailService : IEmailService
    {
        private readonly AppSettings _appSettings;

        public EmailService(AppSettings appSettings)
        {
            _appSettings = appSettings;
        }

        public async Task<bool> SendMeetingResultsAsync(MeetingRecord meetingRecord, EmailSettings settings)
        {
            try
            {
                var emailContent = await GenerateEmailContentAsync(meetingRecord, settings);

                var smtpConfig = _appSettings.EmailConfiguration;

                using var smtpClient = new SmtpClient(smtpConfig.Server, smtpConfig.Port)
                {
                    EnableSsl = smtpConfig.UseSsl,
                    Timeout = smtpConfig.Timeout,
                    UseDefaultCredentials = smtpConfig.UseDefaultCredentials
                };

                // Настройка аутентификации
                if (!smtpConfig.UseDefaultCredentials)
                {
                    smtpClient.Credentials = new NetworkCredential(
                        smtpConfig.Username,
                        smtpConfig.Password
                    );
                }

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(
                        smtpConfig.SenderEmail,
                        smtpConfig.SenderName
                    ),
                    Subject = settings.Subject ?? $"Результаты встречи: {meetingRecord.Title}",
                    Body = emailContent,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(settings.RecipientEmail);

                await smtpClient.SendMailAsync(mailMessage);
                return true;
            }
            catch (Exception ex)
            {
                // Расширенное логирование
                Console.WriteLine($"Ошибка отправки email: {ex.Message}");
                Console.WriteLine($"Детали: {ex.StackTrace}");
                return false;
            }
        }

        public async Task<string> GenerateEmailContentAsync(MeetingRecord meetingRecord, EmailSettings settings)
        {
            var sb = new StringBuilder();

            // Заголовок письма
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html>");
            sb.AppendLine("<head>");
            sb.AppendLine("<meta charset=\"UTF-8\">");
            sb.AppendLine("<style>");
            sb.AppendLine("body { font-family: Arial, sans-serif; margin: 0; padding: 20px; }");
            sb.AppendLine("h1, h2, h3 { color: #333; }");
            sb.AppendLine(".meeting-info { background-color: #f5f5f5; padding: 15px; border-radius: 5px; margin-bottom: 20px; }");
            sb.AppendLine(".summary { background-color: #e6f7ff; padding: 15px; border-radius: 5px; margin-bottom: 20px; }");
            sb.AppendLine(".commitment { background-color: #fff; padding: 10px; border-left: 3px solid #1890ff; margin-bottom: 10px; }");
            sb.AppendLine(".commitment-responsible { font-weight: bold; }");
            sb.AppendLine(".commitment-deadline { color: #ff4d4f; }");
            sb.AppendLine(".transcription { background-color: #f9f9f9; padding: 15px; border-radius: 5px; }");
            sb.AppendLine(".speaker { margin-bottom: 10px; }");
            sb.AppendLine("</style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");

            // Информация о встрече
            sb.AppendLine("<div class=\"meeting-info\">");
            sb.AppendLine($"<h1>Результаты встречи: {meetingRecord.Title}</h1>");
            sb.AppendLine($"<p><strong>Дата:</strong> {meetingRecord.RecordedAt:dd.MM.yyyy HH:mm}</p>");
            sb.AppendLine($"<p><strong>Длительность:</strong> {TimeSpan.FromSeconds(meetingRecord.Duration):hh\\:mm\\:ss}</p>");
            sb.AppendLine("</div>");

            // Краткий пересказ
            if (settings.IncludeSummary && meetingRecord.Summary != null)
            {
                sb.AppendLine("<h2>Краткий пересказ встречи</h2>");
                sb.AppendLine("<div class=\"summary\">");
                sb.AppendLine($"<p>{meetingRecord.Summary.Summary}</p>");

                if (meetingRecord.Summary.KeyTopics.Count > 0)
                {
                    sb.AppendLine("<h3>Ключевые темы:</h3>");
                    sb.AppendLine("<ul>");
                    foreach (var topic in meetingRecord.Summary.KeyTopics)
                    {
                        sb.AppendLine($"<li>{topic}</li>");
                    }
                    sb.AppendLine("</ul>");
                }

                sb.AppendLine("</div>");
            }

            // Обязательства
            if (settings.IncludeCommitments && meetingRecord.Commitments.Count > 0)
            {
                sb.AppendLine("<h2>Выделенные обязательства</h2>");

                foreach (var commitment in meetingRecord.Commitments)
                {
                    sb.AppendLine("<div class=\"commitment\">");
                    sb.AppendLine($"<p>{commitment.Text}</p>");
                    sb.AppendLine($"<p><span class=\"commitment-responsible\">Ответственный: {commitment.ResponsiblePerson}</span></p>");

                    if (commitment.Deadline.HasValue)
                    {
                        sb.AppendLine($"<p><span class=\"commitment-deadline\">Срок: {commitment.Deadline.Value:dd.MM.yyyy}</span></p>");
                    }

                    sb.AppendLine("</div>");
                }
            }

            // Полная транскрипция
            if (settings.IncludeTranscription && meetingRecord.Transcription != null)
            {
                sb.AppendLine("<h2>Полная расшифровка встречи</h2>");
                sb.AppendLine("<div class=\"transcription\">");

                foreach (var segment in meetingRecord.Transcription.Segments)
                {
                    string speakerName = !string.IsNullOrEmpty(segment.SpeakerName) ? segment.SpeakerName : $"Говорящий {segment.SpeakerId}";

                    sb.AppendLine("<div class=\"speaker\">");
                    sb.AppendLine($"<strong>{speakerName} ({TimeSpan.FromSeconds(segment.StartTime):mm\\:ss}):</strong> {segment.Text}");
                    sb.AppendLine("</div>");
                }

                sb.AppendLine("</div>");
            }

            // Подпись
            sb.AppendLine("<hr>");
            sb.AppendLine("<p>Это письмо сгенерировано автоматически приложением \"Мобильный ассистент\".</p>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            return sb.ToString();
        }
    }
}