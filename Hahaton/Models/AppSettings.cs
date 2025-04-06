// Models/AppSettings.cs
namespace MobileAssistant.Models
{
    public class AppSettings
    {
        // Настройки для Yandex API
        public string YandexApiKey { get; set; }
        public string YandexKeyId { get; set; }
        public string YandexFolderId { get; set; }

        // Общие настройки
        public int MaxRecordingDurationSeconds { get; set; } = 600;
        public string AudioFormat { get; set; } = "wav";
        public int AudioBitrate { get; set; } = 1451000;

        // Настройки электронной почты
        public EmailSettings DefaultEmailSettings { get; set; } = new EmailSettings();

        // Для обратной совместимости
        public string SpeechRecognitionApiKey => YandexApiKey;
        public string SpeechRecognitionEndpoint => "https://stt.api.cloud.yandex.net/speech/v1/stt:recognize";
        public string GigaChatApiToken { get; set; }
        public string GigaChatApiEndpoint { get; set; } = "https://ai.rt.ru/api/1.0/gigachat/chat";

        // Новый раздел настроек электронной почты
        public SmtpSettings EmailConfiguration { get; set; } = new SmtpSettings();

    }
}