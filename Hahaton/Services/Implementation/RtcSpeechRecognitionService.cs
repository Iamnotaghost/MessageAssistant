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
    /// <summary>
    /// Реализация сервиса распознавания речи с использованием Нейрошлюза Ростелеком
    /// </summary>
    public class RtcSpeechRecognitionService : ISpeechRecognitionService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _endpoint;
        private CancellationTokenSource _cancellationTokenSource;

        public event EventHandler<int> TranscriptionProgressChanged;

        public RtcSpeechRecognitionService(IHttpClientFactory httpClientFactory, AppSettings appSettings)
        {
            _httpClient = httpClientFactory.CreateClient("RtcApi");
            _apiKey = appSettings.SpeechRecognitionApiKey;
            _endpoint = appSettings.SpeechRecognitionEndpoint;

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            Console.WriteLine($"DEBUG: Инициализирован сервис распознавания речи Ростелеком");
            Console.WriteLine($"DEBUG: Endpoint: {_endpoint}");
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

        public async Task<ApiResponse<TranscriptionResult>> CheckTranscriptionStatusAsync(string transcriptionId)
        {
            try
            {
                Console.WriteLine($"DEBUG: Проверка статуса транскрипции: {transcriptionId}");

                var response = await _httpClient.GetAsync($"{_endpoint}/status/{transcriptionId}");

                if (!response.IsSuccessStatusCode)
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"DEBUG: Ошибка при проверке статуса: {response.StatusCode}, {errorContent}");

                    return new ApiResponse<TranscriptionResult>
                    {
                        Success = false,
                        ErrorMessage = $"Ошибка при проверке статуса: {response.StatusCode}, {errorContent}"
                    };
                }

                var content = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"DEBUG: Получен ответ статуса: {content.Substring(0, Math.Min(100, content.Length))}...");

                var result = JsonSerializer.Deserialize<ApiResponse<TranscriptionStatus>>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                // Преобразуем ответ в нужный формат
                if (result.Result.Status == "completed" && result.Result.Result != null)
                {
                    return new ApiResponse<TranscriptionResult>
                    {
                        Success = true,
                        Result = result.Result.Result
                    };
                }
                else if (result.Result.Status == "processing")
                {
                    TranscriptionProgressChanged?.Invoke(this, result.Result.Progress);

                    return new ApiResponse<TranscriptionResult>
                    {
                        Success = true,
                        Result = null
                    };
                }
                else
                {
                    return new ApiResponse<TranscriptionResult>
                    {
                        Success = false,
                        ErrorMessage = $"Неизвестный статус: {result.Result.Status}"
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DEBUG: Ошибка при проверке статуса транскрипции: {ex.Message}");

                return new ApiResponse<TranscriptionResult>
                {
                    Success = false,
                    ErrorMessage = $"Ошибка при проверке статуса транскрипции: {ex.Message}"
                };
            }
        }

        private async Task<TranscriptionResult> TranscribeInternalAsync(string audioFilePath, bool diarize)
        {
            if (string.IsNullOrEmpty(audioFilePath) || !File.Exists(audioFilePath))
            {
                throw new FileNotFoundException("Аудиофайл не найден", audioFilePath);
            }

            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                Console.WriteLine($"DEBUG: Начинаем транскрибировать файл: {audioFilePath}, с диаризацией: {diarize}");

                // Отправляем файл на транскрибацию
                var fileBytes = await File.ReadAllBytesAsync(audioFilePath);
                var fileName = Path.GetFileName(audioFilePath);

                using var content = new MultipartFormDataContent();
                using var fileContent = new ByteArrayContent(fileBytes);
                fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(GetContentType(audioFilePath));
                content.Add(fileContent, "file", fileName);
                content.Add(new StringContent(diarize.ToString().ToLowerInvariant()), "diarize");
                content.Add(new StringContent("ru-RU"), "language"); // Русский язык

                var url = $"{_endpoint}/transcribe";

                Console.WriteLine($"DEBUG: Отправляем запрос на {url}");

                // Отправляем запрос на транскрибацию
                var response = await _httpClient.PostAsync(url, content, _cancellationTokenSource.Token);

                if (!response.IsSuccessStatusCode)
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"DEBUG: Ошибка при отправке запроса: {response.StatusCode}, {errorContent}");
                    throw new Exception($"Ошибка при отправке запроса: {response.StatusCode}, {errorContent}");
                }

                // Получаем ID транскрипции для проверки статуса
                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"DEBUG: Получен ответ: {responseContent}");

                var transcriptionResponse = JsonSerializer.Deserialize<TranscriptionResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (transcriptionResponse.Status == "processing")
                {
                    // Периодически проверяем статус транскрипции
                    Console.WriteLine($"DEBUG: Задача на обработку принята, ID: {transcriptionResponse.Id}");
                    return await PollTranscriptionStatusAsync(transcriptionResponse.Id);
                }
                else if (transcriptionResponse.Status == "completed")
                {
                    // Транскрипция уже готова
                    Console.WriteLine($"DEBUG: Транскрипция сразу готова");
                    return transcriptionResponse.Result;
                }
                else
                {
                    throw new Exception($"Неожиданный статус транскрипции: {transcriptionResponse.Status}");
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("DEBUG: Транскрипция была отменена");
                throw new OperationCanceledException("Транскрипция была отменена");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DEBUG: Ошибка при транскрибации аудио: {ex.Message}");
                throw new Exception($"Ошибка при транскрибации аудио: {ex.Message}", ex);
            }
            finally
            {
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        private async Task<TranscriptionResult> PollTranscriptionStatusAsync(string transcriptionId)
        {
            int progress = 0;

            Console.WriteLine($"DEBUG: Начинаем опрос статуса транскрипции: {transcriptionId}");

            // Проверяем статус каждые 3 секунды
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    var statusResponse = await CheckTranscriptionStatusAsync(transcriptionId);

                    if (!statusResponse.Success)
                    {
                        Console.WriteLine($"DEBUG: Ошибка при проверке статуса: {statusResponse.ErrorMessage}");
                        throw new Exception(statusResponse.ErrorMessage);
                    }

                    if (statusResponse.Result != null)
                    {
                        Console.WriteLine("DEBUG: Транскрипция завершена");
                        return statusResponse.Result;
                    }

                    // Ждем перед следующей проверкой
                    await Task.Delay(3000, _cancellationTokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("DEBUG: Опрос статуса был отменен");
                    throw;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"DEBUG: Ошибка при опросе статуса: {ex.Message}");
                    throw new Exception($"Ошибка при опросе статуса транскрипции: {ex.Message}", ex);
                }
            }

            throw new OperationCanceledException("Опрос статуса транскрипции был отменен");
        }

        private string GetContentType(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLowerInvariant();

            return extension switch
            {
                ".wav" => "audio/wav",
                ".mp3" => "audio/mpeg",
                ".ogg" => "audio/ogg",
                ".m4a" => "audio/mp4",
                _ => "application/octet-stream"
            };
        }

        // Вспомогательные классы для десериализации ответов API
        public class TranscriptionResponse
        {
            public string Id { get; set; }
            public string Status { get; set; }
            public TranscriptionResult Result { get; set; }
        }

        public class TranscriptionStatus
        {
            public string Status { get; set; }
            public int Progress { get; set; }
            public TranscriptionResult Result { get; set; }
        }
    }
}