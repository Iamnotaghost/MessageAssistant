// Реализация сервиса для записи и работы с аудио
using MobileAssistant.Services.Interfaces;
using Plugin.Maui.Audio;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MobileAssistant.Services.Implementation
{
    public class AudioService : IAudioService
    {
        private readonly IAudioManager _audioManager;
        private IAudioRecorder _recorder;
        private string _filePath;
        private Timer _durationTimer;
        private double _currentDuration = 0;
        private readonly int _timerInterval = 100; // миллисекунды

        public bool IsRecording { get; private set; }
        public double CurrentRecordingDuration => _currentDuration;

        public event EventHandler<double> RecordingDurationChanged;

        public AudioService(IAudioManager audioManager)
        {
            _audioManager = audioManager;
            Console.WriteLine("DEBUG: AudioService инициализирован");
        }

        public async Task StartRecordingAsync()
        {
            if (IsRecording)
                return;

            try
            {
                Console.WriteLine("DEBUG: Начинаем запись аудио");

                // Создаем уникальное имя файла на основе текущей даты и времени
                string fileName = $"meeting_{DateTime.Now:yyyyMMdd_HHmmss}.wav";
                _filePath = Path.Combine(FileSystem.CacheDirectory, fileName);

                Console.WriteLine($"DEBUG: Путь к файлу записи: {_filePath}");

                // Проверяем доступ к директории
                var directory = Path.GetDirectoryName(_filePath);
                EnsureDirectoryExists(directory);

                // Инициализируем и запускаем рекордер
                _recorder = _audioManager.CreateRecorder();

                // Запускаем запись
                await _recorder.StartAsync(_filePath);

                IsRecording = true;
                _currentDuration = 0;

                // Запускаем таймер для отслеживания длительности записи
                _durationTimer = new Timer(OnTimerTick, null, 0, _timerInterval);

                Console.WriteLine("DEBUG: Запись успешно начата");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DEBUG: Ошибка при начале записи: {ex.Message}");
                Console.WriteLine($"DEBUG: Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        public async Task<string> StopRecordingAsync()
        {
            if (!IsRecording || _recorder == null)
                return null;

            try
            {
                Console.WriteLine("DEBUG: Останавливаем запись");

                // Останавливаем таймер
                _durationTimer?.Dispose();
                _durationTimer = null;

                // Останавливаем запись
                await _recorder.StopAsync();
                IsRecording = false;

                Console.WriteLine($"DEBUG: Запись остановлена, файл: {_filePath}");

                // Проверяем, существует ли файл
                if (File.Exists(_filePath))
                {
                    var fileInfo = new FileInfo(_filePath);
                    Console.WriteLine($"DEBUG: Размер файла: {fileInfo.Length} байт");
                }
                else
                {
                    Console.WriteLine("DEBUG: ВНИМАНИЕ! Файл записи не найден после остановки");
                }

                return _filePath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DEBUG: Ошибка при остановке записи: {ex.Message}");
                Console.WriteLine($"DEBUG: Stack trace: {ex.StackTrace}");
                throw;
            }
            finally
            {
                _recorder = null;
            }
        }

        public async Task<string> PickAudioFileAsync()
        {
            try
            {
                Console.WriteLine("DEBUG: Открываем файловый выбор для аудио");

                var options = new PickOptions
                {
                    FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                    {
                        { DevicePlatform.Android, new[] { "audio/wav", "audio/mp3", "audio/mpeg", "audio/ogg", "audio/mp4", "audio/x-wav" } },
                        { DevicePlatform.iOS, new[] { "public.audio" } },
                        { DevicePlatform.WinUI, new[] { ".wav", ".mp3", ".ogg", ".m4a" } }
                    }),
                    PickerTitle = "Выберите аудиофайл"
                };

                var result = await FilePicker.PickAsync(options);

                if (result != null)
                {
                    Console.WriteLine($"DEBUG: Выбран файл: {result.FileName}, путь: {result.FullPath}");

                    // Проверяем, существует ли файл
                    if (File.Exists(result.FullPath))
                    {
                        var fileInfo = new FileInfo(result.FullPath);
                        Console.WriteLine($"DEBUG: Размер файла: {fileInfo.Length} байт");
                    }
                    else
                    {
                        Console.WriteLine("DEBUG: ВНИМАНИЕ! Выбранный файл не найден");
                    }

                    return result.FullPath;
                }
                else
                {
                    Console.WriteLine("DEBUG: Выбор файла отменен пользователем");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DEBUG: Ошибка при выборе аудиофайла: {ex.Message}");
                Console.WriteLine($"DEBUG: Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        public async Task<double> GetAudioDurationAsync(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                Console.WriteLine($"DEBUG: Аудиофайл не найден: {filePath}");
                throw new FileNotFoundException("Аудиофайл не найден", filePath);
            }

            try
            {
                Console.WriteLine($"DEBUG: Определение длительности аудио: {filePath}");

                // Используем IAudioPlayer для получения длительности
                var player = _audioManager.CreatePlayer(filePath);
                var duration = player.Duration;
                player.Dispose();

                Console.WriteLine($"DEBUG: Длительность аудио: {duration} секунд");

                return duration;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DEBUG: Ошибка при определении длительности аудио: {ex.Message}");

                // Если не удалось определить длительность, оцениваем её по размеру файла
                // (примерная оценка для WAV файла: 16-битный стерео, 44.1 кГц)
                try
                {
                    var fileInfo = new FileInfo(filePath);
                    var estimatedDuration = fileInfo.Length / (44100.0 * 2 * 2); // размер / (частота * каналы * байт на семпл)
                    Console.WriteLine($"DEBUG: Оценочная длительность: {estimatedDuration} секунд");
                    return estimatedDuration;
                }
                catch
                {
                    // Если и это не удалось, возвращаем значение по умолчанию
                    return 0;
                }
            }
        }

        private void OnTimerTick(object state)
        {
            // Увеличиваем значение текущей длительности
            _currentDuration += _timerInterval / 1000.0;

            // Уведомляем подписчиков об изменении длительности
            RecordingDurationChanged?.Invoke(this, _currentDuration);
        }

        private void EnsureDirectoryExists(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                try
                {
                    Directory.CreateDirectory(directoryPath);
                    Console.WriteLine($"DEBUG: Создана директория: {directoryPath}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"DEBUG: Ошибка при создании директории: {ex.Message}");
                }
            }
        }
    }
}