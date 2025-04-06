using MobileAssistant.Models;
using MobileAssistant.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MobileAssistant.Services.Implementation
{
    public class YandexSpeechRecognitionService : ISpeechRecognitionService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _folderId;
        private CancellationTokenSource _cancellationTokenSource;

        public event EventHandler<int> TranscriptionProgressChanged;

        public YandexSpeechRecognitionService(IHttpClientFactory httpClientFactory, AppSettings appSettings)
        {
            _httpClient = httpClientFactory.CreateClient("YandexApi");
            _apiKey = appSettings.YandexApiKey;
            _folderId = appSettings.YandexFolderId;

            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Api-Key {_apiKey}");

            Console.WriteLine("DEBUG: YandexSpeechRecognitionService инициализирован");
        }

        public async Task<TranscriptionResult> TranscribeAudioAsync(string audioFilePath)
        {
            return await TranscribeInternalAsync(audioFilePath, false);
        }

        public async Task<TranscriptionResult> TranscribeAudioWithDiarizationAsync(string audioFilePath)
        {
            return await TranscribeInternalAsync(audioFilePath, true);
        }

        public Task CancelTranscriptionAsync()
        {
            _cancellationTokenSource?.Cancel();
            return Task.CompletedTask;
        }

        public Task<ApiResponse<TranscriptionResult>> CheckTranscriptionStatusAsync(string transcriptionId)
        {
            // Yandex SpeechKit не имеет асинхронного API, результат возвращается сразу
            return Task.FromResult(new ApiResponse<TranscriptionResult>
            {
                Success = true,
                Result = null
            });
        }

        private async Task<TranscriptionResult> TranscribeInternalAsync(string audioFilePath, bool diarize)
        {
            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                Console.WriteLine($"DEBUG: Начинаем распознавание аудио: {audioFilePath}");

                if (!File.Exists(audioFilePath))
                {
                    throw new FileNotFoundException($"Аудиофайл не найден: {audioFilePath}");
                }

                // Чтение аудиофайла
                var audioBytes = await File.ReadAllBytesAsync(audioFilePath);

                // Формируем URL с параметрами
                string url = "https://stt.api.cloud.yandex.net/speech/v1/stt:recognize";
                if (!string.IsNullOrEmpty(_folderId))
                {
                    url += $"?folderId={_folderId}";
                }

                if (diarize)
                {
                    url += $"{(url.Contains("?") ? "&" : "?")}format=lpcm&sampleRateHertz=16000&rawResults=true";
                }

                // Отправляем запрос
                using var content = new ByteArrayContent(audioBytes);
                content.Headers.ContentType = new MediaTypeHeaderValue("audio/x-pcm");

                TranscriptionProgressChanged?.Invoke(this, 10);

                var response = await _httpClient.PostAsync(url, content, _cancellationTokenSource.Token);
                response.EnsureSuccessStatusCode();

                TranscriptionProgressChanged?.Invoke(this, 70);

                // Получаем результат
                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"DEBUG: Получен ответ: {responseContent.Substring(0, Math.Min(100, responseContent.Length))}...");

                // Парсим результат
                var recognitionResult = JsonSerializer.Deserialize<YandexRecognitionResult>(
                    responseContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                // Формируем результат транскрипции
                var transcriptionResult = new TranscriptionResult
                {
                    Id = Guid.NewGuid().ToString(),
                    FullText = recognitionResult.Result,
                    CreatedAt = DateTime.Now,
                    Duration = await GetAudioDurationAsync(audioFilePath)
                };

                // Разделяем текст на сегменты (примитивная диаризация)
                if (diarize)
                {
                    transcriptionResult.Segments = CreateSegments(recognitionResult.Result);
                }
                else
                {
                    transcriptionResult.Segments = new List<Segment>
                    {
                        new Segment
                        {
                            Text = recognitionResult.Result,
                            SpeakerId = 1,
                            SpeakerName = "Говорящий 1",
                            StartTime = 0,
                            EndTime = transcriptionResult.Duration
                        }
                    };
                }

                TranscriptionProgressChanged?.Invoke(this, 100);

                Console.WriteLine("DEBUG: Распознавание аудио завершено");
                return transcriptionResult;
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("DEBUG: Распознавание аудио отменено");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DEBUG: Ошибка при распознавании аудио: {ex.Message}");
                throw;
            }
            finally
            {
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        private List<Segment> CreateSegments(string text)
        {
            // Простое разделение текста на предложения
            var sentences = text.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
            var segments = new List<Segment>();

            // Если предложений меньше 2, просто возвращаем один сегмент
            if (sentences.Length <= 2)
            {
                segments.Add(new Segment
                {
                    Text = text,
                    SpeakerId = 1,
                    SpeakerName = "Говорящий 1",
                    StartTime = 0,
                    EndTime = 0
                });

                return segments;
            }

            // Иначе делим на сегменты и чередуем говорящих
            int segmentCount = Math.Min(10, sentences.Length);
            int sentencesPerSegment = sentences.Length / segmentCount;

            for (int i = 0; i < segmentCount; i++)
            {
                int start = i * sentencesPerSegment;
                int end = (i == segmentCount - 1) ? sentences.Length : (i + 1) * sentencesPerSegment;

                string segmentText = string.Join(". ", sentences[start..end]) + ".";

                segments.Add(new Segment
                {
                    Text = segmentText,
                    SpeakerId = (i % 2) + 1, // Чередуем ID говорящих (1 и 2)
                    SpeakerName = $"Говорящий {(i % 2) + 1}",
                    StartTime = 0, // Без временных меток
                    EndTime = 0
                });
            }

            return segments;
        }

        private async Task<double> GetAudioDurationAsync(string audioFilePath)
        {
            try
            {
                var info = new FileInfo(audioFilePath);
                // Примерный расчет для WAV 16-bit, 44.1 kHz, stereo
                return info.Length / (44100.0 * 2 * 2);
            }
            catch
            {
                return 0;
            }
        }

        private class YandexRecognitionResult
        {
            public string Result { get; set; }
        }
    }
}