using MobileAssistant.Models;

namespace MobileAssistant.Helpers
{
    public class RostelecomApiTester
    {
        private readonly HttpClient _httpClient;
        private readonly AppSettings _appSettings;

        public RostelecomApiTester(HttpClient httpClient, AppSettings appSettings)
        {
            _httpClient = httpClient;
            _appSettings = appSettings;
        }

        public async Task<bool> TestSpeechApiConnectionAsync()
        {
            try
            {
                Console.WriteLine("DEBUG: Проверка соединения с API распознавания речи Ростелеком");

                // Установка заголовков авторизации
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_appSettings.SpeechRecognitionApiKey}");

                // Тестовый запрос (например, запрос доступных моделей)
                var response = await _httpClient.GetAsync($"{_appSettings.SpeechRecognitionEndpoint}/models");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"DEBUG: Успешное соединение с API распознавания речи: {content}");
                    return true;
                }
                else
                {
                    Console.WriteLine($"DEBUG: Ошибка соединения с API распознавания речи: {response.StatusCode}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DEBUG: Исключение при проверке API распознавания речи: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> TestLlmApiConnectionAsync()
        {
            try
            {
                Console.WriteLine("DEBUG: Проверка соединения с API языковой модели Ростелеком");

                // Установка заголовков авторизации
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_appSettings.GigaChatApiToken}");

                // Тестовый запрос (например, запрос доступных моделей)
                var response = await _httpClient.GetAsync($"{_appSettings.GigaChatApiEndpoint}/models");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"DEBUG: Успешное соединение с API языковой модели: {content}");
                    return true;
                }
                else
                {
                    Console.WriteLine($"DEBUG: Ошибка соединения с API языковой модели: {response.StatusCode}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DEBUG: Исключение при проверке API языковой модели: {ex.Message}");
                return false;
            }
        }
    }
}