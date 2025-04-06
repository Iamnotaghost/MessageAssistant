using MobileAssistant.Models;
using MobileAssistant.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MobileAssistant.Services.Implementation
{
    /// <summary>
    /// Реализация сервиса обработки текста с использованием нейрошлюза ai.rt.ru (GigaChat API)
    /// </summary>
    public class YandexGptServices : ITextProcessingService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _endpoint;
        private CancellationTokenSource _cancellationTokenSource;
        private string _currentChatUuid;

        public YandexGptServices(IHttpClientFactory httpClientFactory, AppSettings appSettings)
        {
            _httpClient = httpClientFactory.CreateClient("RtcApi");
            _apiKey = appSettings.GigaChatApiToken;
            _endpoint = appSettings.GigaChatApiEndpoint;

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            Console.WriteLine($"DEBUG: Инициализирован сервис обработки текста Нейрошлюз GigaChat");
            Console.WriteLine($"DEBUG: Endpoint: {_endpoint}");
        }

        public async Task<SummaryResult> SummarizeTranscriptionAsync(TranscriptionResult transcription)
        {
            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                Console.WriteLine("DEBUG: Начинаем суммаризацию текста через GigaChat");

                var text = ConvertTranscriptionToText(transcription);

                // Промпт для суммаризации
                string prompt = @"
                Сделай краткий и структурированный пересказ (не более 500 слов) следующей транскрипции встречи.
                Выдели ключевые темы обсуждения, основные решения и важные моменты.
                Формат вывода:

                Краткий пересказ встречи:
                [Твой пересказ здесь]

                Ключевые темы:
                - Тема 1
                - Тема 2
                ...
                ";

                // Формируем запрос к GigaChat API
                var messages = new List<object>
                {
                    new { role = "system", content = prompt },
                    new { role = "user", content = text }
                };

                var response = await SendLlmRequestAsync(messages, _cancellationTokenSource.Token);

                // Парсим результат для извлечения пересказа и ключевых тем
                string summary = response.Message.Content;
                List<string> keyTopics = ExtractKeyTopics(summary);

                return new SummaryResult
                {
                    Id = Guid.NewGuid().ToString(),
                    Summary = summary,
                    KeyTopics = keyTopics,
                    TranscriptionId = transcription.Id,
                    CreatedAt = DateTime.Now
                };
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("DEBUG: Суммаризация была отменена");
                throw new OperationCanceledException("Суммаризация была отменена");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DEBUG: Ошибка при суммаризации: {ex.Message}");
                throw new Exception($"Ошибка при суммаризации транскрипции: {ex.Message}", ex);
            }
            finally
            {
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        public async Task<List<Commitment>> ExtractCommitmentsAsync(TranscriptionResult transcription)
        {
            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                Console.WriteLine("DEBUG: Начинаем извлечение обязательств через GigaChat");

                var text = ConvertTranscriptionToText(transcription);

                // Промпт для извлечения обязательств
                string prompt = @"
                Проанализируй текст встречи и выдели все обязательства, которые взяли на себя участники.
                Для каждого обязательства определи:
                1. Текст обязательства (что конкретно нужно сделать)
                2. Ответственное лицо (кто взял на себя обязательство)
                3. Срок выполнения (если указан)
                
                Формат ответа: строго JSON-массив объектов с полями:
                {
                   ""text"": ""текст обязательства"",
                   ""responsiblePerson"": ""ответственное лицо"",
                   ""deadline"": ""YYYY-MM-DD"" или null, если срок не указан
                }
                
                Выдай только валидный JSON без дополнительных комментариев и форматирования.
                ";

                // Формируем запрос к GigaChat API
                var messages = new List<object>
                {
                    new { role = "system", content = prompt },
                    new { role = "user", content = text }
                };

                var response = await SendLlmRequestAsync(messages, _cancellationTokenSource.Token);

                // Парсим JSON из ответа
                string jsonResponse = response.Message.Content;
                jsonResponse = CleanJsonString(jsonResponse);

                try
                {
                    var commitmentDtos = JsonSerializer.Deserialize<List<CommitmentDto>>(
                        jsonResponse,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    // Преобразуем DTO в модель обязательств
                    var commitments = new List<Commitment>();

                    foreach (var dto in commitmentDtos)
                    {
                        DateTime? deadline = null;
                        if (!string.IsNullOrEmpty(dto.Deadline))
                        {
                            if (DateTime.TryParse(dto.Deadline, out var date))
                            {
                                deadline = date;
                            }
                        }

                        commitments.Add(new Commitment
                        {
                            Id = Guid.NewGuid().ToString(),
                            Text = dto.Text,
                            ResponsiblePerson = dto.ResponsiblePerson,
                            Deadline = deadline,
                            TranscriptionId = transcription.Id,
                            Status = CommitmentStatus.Pending,
                            CreatedAt = DateTime.Now
                        });
                    }

                    Console.WriteLine($"DEBUG: Извлечено {commitments.Count} обязательств");

                    return commitments;
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"DEBUG: Ошибка парсинга JSON: {ex.Message}");
                    Console.WriteLine($"DEBUG: Полученный JSON: {jsonResponse}");

                    // Если произошла ошибка парсинга, возвращаем пустой список
                    return new List<Commitment>();
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("DEBUG: Извлечение обязательств было отменено");
                throw new OperationCanceledException("Извлечение обязательств было отменено");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DEBUG: Ошибка при извлечении обязательств: {ex.Message}");
                throw new Exception($"Ошибка при извлечении обязательств: {ex.Message}", ex);
            }
            finally
            {
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        public async Task<List<SpeakerInfo>> IdentifySpeakersAsync(TranscriptionResult transcription)
        {
            try
            {
                Console.WriteLine("DEBUG: Начинаем идентификацию говорящих");

                // Получаем уникальные ID говорящих из сегментов
                var speakerIds = transcription.Segments
                    .Select(s => s.SpeakerId)
                    .Distinct()
                    .ToList();

                var speakers = new List<SpeakerInfo>();

                // Генерируем уникальные цвета для каждого говорящего
                var colors = GenerateUniqueColors(speakerIds.Count);

                // Создаем объекты SpeakerInfo для каждого говорящего
                for (int i = 0; i < speakerIds.Count; i++)
                {
                    var id = speakerIds[i];
                    speakers.Add(new SpeakerInfo
                    {
                        Id = id,
                        Name = $"Говорящий {id}",
                        ColorHex = colors[i],
                        MeetingId = transcription.Id
                    });
                }

                Console.WriteLine($"DEBUG: Идентифицировано {speakers.Count} говорящих");

                return speakers;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DEBUG: Ошибка при идентификации говорящих: {ex.Message}");
                throw new Exception($"Ошибка при идентификации говорящих: {ex.Message}", ex);
            }
        }

        public Task CancelProcessingAsync()
        {
            _cancellationTokenSource?.Cancel();
            return Task.CompletedTask;
        }

        // Вспомогательные методы

        private async Task<GigaChatResponse> SendLlmRequestAsync(List<object> messages, CancellationToken cancellationToken)
        {
            var requestBody = new
            {
                uuid = _currentChatUuid, // null при первом запросе, потом используем UUID чата
                chat = new
                {
                    messages = messages,
                    model = "GigaChat",
                    temperature = 0.3,
                    top_p = 0.9,
                    n = 1,
                    max_tokens = 1500,
                    repetition_penalty = 1.0
                }
            };

            var content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json"
            );

            Console.WriteLine($"DEBUG: Отправляем запрос на {_endpoint}");

            var response = await _httpClient.PostAsync(_endpoint, content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                string errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                Console.WriteLine($"DEBUG: Ошибка при отправке запроса: {response.StatusCode}, {errorContent}");
                throw new Exception($"Ошибка при отправке запроса к LLM: {response.StatusCode}, {errorContent}");
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            Console.WriteLine($"DEBUG: Получен ответ от LLM длиной {responseContent.Length} символов");

            // Десериализуем массив с единственным элементом
            var responseArray = JsonSerializer.Deserialize<GigaChatResponse[]>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (responseArray == null || responseArray.Length == 0)
            {
                throw new Exception("Получен пустой ответ от API");
            }

            // Сохраняем UUID чата для последующих запросов
            _currentChatUuid = responseArray[0].Uuid;

            return responseArray[0];
        }

        private string ConvertTranscriptionToText(TranscriptionResult transcription)
        {
            if (!string.IsNullOrEmpty(transcription.FullText))
                return transcription.FullText;

            var sb = new StringBuilder();

            foreach (var segment in transcription.Segments)
            {
                string speakerName = !string.IsNullOrEmpty(segment.SpeakerName)
                    ? segment.SpeakerName
                    : $"Говорящий {segment.SpeakerId}";

                sb.AppendLine($"{speakerName}: {segment.Text}");
            }

            return sb.ToString();
        }

        private List<string> ExtractKeyTopics(string summary)
        {
            try
            {
                var keyTopics = new List<string>();

                // Ищем ключевые темы после маркера
                int keyTopicsIndex = summary.IndexOf("Ключевые темы:", StringComparison.OrdinalIgnoreCase);

                if (keyTopicsIndex >= 0)
                {
                    // Выделяем часть текста после маркера
                    string topicsSection = summary.Substring(keyTopicsIndex + "Ключевые темы:".Length);

                    // Разбиваем на строки
                    var lines = topicsSection.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                    // Ищем строки, начинающиеся с маркеров списка
                    foreach (var line in lines)
                    {
                        string trimmedLine = line.Trim();
                        if (trimmedLine.StartsWith("-") || trimmedLine.StartsWith("•"))
                        {
                            keyTopics.Add(trimmedLine.Substring(1).Trim());
                        }
                    }
                }

                return keyTopics;
            }
            catch
            {
                // В случае ошибки возвращаем пустой список
                return new List<string>();
            }
        }

        private string CleanJsonString(string jsonString)
        {
            // Очистка от маркеров кода, если они есть
            if (jsonString.StartsWith("```json"))
            {
                jsonString = jsonString.Substring("```json".Length);
            }
            else if (jsonString.StartsWith("```"))
            {
                jsonString = jsonString.Substring("```".Length);
            }

            if (jsonString.EndsWith("```"))
            {
                jsonString = jsonString.Substring(0, jsonString.Length - "```".Length);
            }

            // Ищем начало и конец JSON массива
            int startIndex = jsonString.IndexOf('[');
            int endIndex = jsonString.LastIndexOf(']') + 1;

            if (startIndex >= 0 && endIndex > startIndex)
            {
                return jsonString.Substring(startIndex, endIndex - startIndex);
            }

            return jsonString.Trim();
        }

        private List<string> GenerateUniqueColors(int count)
        {
            var colors = new List<string>();
            var random = new Random();

            for (int i = 0; i < count; i++)
            {
                int hue = i * 360 / count;  // Равномерно распределяем оттенки по цветовому кругу

                // Преобразуем HSV в RGB
                double h = hue / 360.0;
                double s = 0.7;  // Насыщенность
                double v = 0.9;  // Яркость

                // Алгоритм преобразования HSV в RGB
                double r, g, b;

                int hi = Convert.ToInt32(Math.Floor(h * 6)) % 6;
                double f = h * 6 - Math.Floor(h * 6);
                double p = v * (1 - s);
                double q = v * (1 - f * s);
                double t = v * (1 - (1 - f) * s);

                switch (hi)
                {
                    case 0: r = v; g = t; b = p; break;
                    case 1: r = q; g = v; b = p; break;
                    case 2: r = p; g = v; b = t; break;
                    case 3: r = p; g = q; b = v; break;
                    case 4: r = t; g = p; b = v; break;
                    default: r = v; g = p; b = q; break;
                }

                // Преобразуем в HEX
                string hex = $"#{(int)(r * 255):X2}{(int)(g * 255):X2}{(int)(b * 255):X2}";
                colors.Add(hex);
            }

            return colors;
        }

        // Вспомогательные классы

        private class GigaChatResponse
        {
            public string Uuid { get; set; }
            public GigaChatMessage Message { get; set; }
        }

        private class GigaChatMessage
        {
            public int Id { get; set; }
            public string Role { get; set; }
            public string Content { get; set; }
            public string Type { get; set; }
            public long Created { get; set; }
        }

        private class CommitmentDto
        {
            public string Text { get; set; }
            public string ResponsiblePerson { get; set; }
            public string Deadline { get; set; }
        }
    }
}