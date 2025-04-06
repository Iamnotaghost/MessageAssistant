namespace MobileAssistant.Models
{
    public class EmailSettings
    {
        /// <summary>
        /// Email-адрес получателя
        /// </summary>
        public string RecipientEmail { get; set; }

        /// <summary>
        /// Тема email
        /// </summary>
        public string Subject { get; set; } = "Результаты обработки встречи";

        /// <summary>
        /// Включать ли полную транскрипцию в email
        /// </summary>
        public bool IncludeTranscription { get; set; } = true;

        /// <summary>
        /// Включать ли краткий пересказ в email
        /// </summary>
        public bool IncludeSummary { get; set; } = true;

        /// <summary>
        /// Включать ли список обязательств в email
        /// </summary>
        public bool IncludeCommitments { get; set; } = true;
    }

    public class SmtpSettings
    {
        // Настройки SMTP-сервера
        public string Server { get; set; }
        public int Port { get; set; } = 587;
        public bool UseSsl { get; set; } = true;

        // Аутентификация
        public string Username { get; set; }
        public string Password { get; set; }

        // Отправитель
        public string SenderName { get; set; } = "Meeting Assistant";
        public string SenderEmail { get; set; }

        // Дополнительные настройки
        public int Timeout { get; set; } = 10000; // 10 секунд
        public bool UseDefaultCredentials { get; set; } = false;
    }
}