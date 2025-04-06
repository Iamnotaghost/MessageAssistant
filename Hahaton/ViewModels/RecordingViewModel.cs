using MobileAssistant.Models;
using MobileAssistant.Services.Interfaces;
using MobileAssistant.Views;
using System;
using System.Threading.Tasks;
using System.Windows.Input;
using System.IO;

namespace MobileAssistant.ViewModels
{
    [QueryProperty(nameof(Mode), "mode")]
    public class RecordingViewModel : BaseViewModel
    {
        private readonly IAudioService _audioService;
        private readonly IMeetingService _meetingService;
        private readonly IDialogService _dialogService;

        private string _mode;
        private string _timerText;
        private bool _isRecording;
        private string _recordButtonText;
        private string _statusText;
        private double _timerValue;

        public string Mode
        {
            get => _mode;
            set => SetProperty(ref _mode, value);
        }

        public string TimerText
        {
            get => _timerText;
            set => SetProperty(ref _timerText, value);
        }

        public bool IsRecording
        {
            get => _isRecording;
            set
            {
                if (SetProperty(ref _isRecording, value))
                {
                    RecordButtonText = value ? "Остановить запись" : "Начать запись";
                    Console.WriteLine($"DEBUG: IsRecording изменился на {value}, обновляем CanSave");
                    OnPropertyChanged(nameof(CanSave));
                    ((Command)SaveRecordingCommand).ChangeCanExecute();
                }
            }
        }

        public string RecordButtonText
        {
            get => _recordButtonText;
            set => SetProperty(ref _recordButtonText, value);
        }

        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        public bool CanSave => !IsRecording && !string.IsNullOrEmpty(_audioFilePath);

        public ICommand ToggleRecordingCommand { get; }
        public ICommand SaveRecordingCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand DebugEnableSaveCommand { get; }

        private string _audioFilePath;
        private string _meetingTitle;

        public RecordingViewModel(IAudioService audioService, IMeetingService meetingService, IDialogService dialogService)
        {
            _audioService = audioService;
            _meetingService = meetingService;
            _dialogService = dialogService;

            Title = "Запись встречи";
            RecordButtonText = "Начать запись";
            TimerText = "00:00";

            // Команды
            ToggleRecordingCommand = new Command(async () => await ToggleRecordingAsync());
            SaveRecordingCommand = new Command(async () => await SaveRecordingAsync(), () => CanSave);
            CancelCommand = new Command(async () => await CancelAsync());
            DebugEnableSaveCommand = new Command(ForceEnableSaveButton);

            // Подписываемся на событие изменения длительности записи
            _audioService.RecordingDurationChanged += OnRecordingDurationChanged;

            Console.WriteLine($"DEBUG: RecordingViewModel создан. CanSave={CanSave}, IsRecording={IsRecording}, аудиофайл пустой={string.IsNullOrEmpty(_audioFilePath)}");
        }

        public async Task OnAppearingAsync()
        {
            Console.WriteLine($"DEBUG: OnAppearingAsync вызван, Mode={Mode}");

            // Проверяем разрешения при открытии страницы
            var audioPermission = await Permissions.RequestAsync<Permissions.Microphone>();
            var storageReadPermission = await Permissions.RequestAsync<Permissions.StorageRead>();
            var storageWritePermission = await Permissions.RequestAsync<Permissions.StorageWrite>();

            Console.WriteLine($"DEBUG: Разрешения: микрофон={audioPermission}, чтение={storageReadPermission}, запись={storageWritePermission}");

            if (Mode == "upload")
            {
                await UploadAudioAsync();
            }
        }

        private async Task ToggleRecordingAsync()
        {
            Console.WriteLine($"DEBUG: ToggleRecordingAsync вызван, IsRecording={IsRecording}");

            if (IsRecording)
            {
                // Останавливаем запись
                try
                {
                    IsRecording = false;
                    StatusText = "Сохранение записи...";

                    Console.WriteLine("DEBUG: Останавливаем запись...");
                    _audioFilePath = await _audioService.StopRecordingAsync();
                    Console.WriteLine($"DEBUG: Запись остановлена, _audioFilePath={_audioFilePath}");

                    if (!string.IsNullOrEmpty(_audioFilePath))
                    {
                        if (File.Exists(_audioFilePath))
                        {
                            var fileInfo = new FileInfo(_audioFilePath);
                            Console.WriteLine($"DEBUG: Файл существует, размер: {fileInfo.Length} байт");
                        }
                        else
                        {
                            Console.WriteLine("DEBUG: ОШИБКА - файл не существует после записи");
                        }
                    }

                    StatusText = "Запись сохранена";
                    OnPropertyChanged(nameof(CanSave));
                    ((Command)SaveRecordingCommand).ChangeCanExecute();

                    Console.WriteLine($"DEBUG: CanSave после остановки записи: {CanSave}");

                    // Запрашиваем название встречи
                    await RequestMeetingTitleAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"DEBUG: Ошибка при остановке записи: {ex.Message}");
                    Console.WriteLine($"DEBUG: Стек вызовов: {ex.StackTrace}");
                    StatusText = $"Ошибка: {ex.Message}";
                    await _dialogService.DisplayAlertAsync("Ошибка", $"Не удалось остановить запись: {ex.Message}", "OK");
                }
            }
            else
            {
                // Начинаем запись
                try
                {
                    // Проверяем разрешения
                    var status = await Permissions.RequestAsync<Permissions.Microphone>();
                    var storageStatus = await Permissions.RequestAsync<Permissions.StorageWrite>();

                    Console.WriteLine($"DEBUG: Разрешения перед записью: микрофон={status}, хранилище={storageStatus}");

                    if (status != PermissionStatus.Granted || storageStatus != PermissionStatus.Granted)
                    {
                        await _dialogService.DisplayAlertAsync("Отказано в доступе", "Для записи аудио необходимо разрешение на использование микрофона и хранилища", "OK");
                        return;
                    }

                    // Сбрасываем значения
                    _audioFilePath = null;
                    _timerValue = 0;
                    TimerText = "00:00";
                    StatusText = "Идет запись...";

                    // Запускаем запись
                    Console.WriteLine("DEBUG: Начинаем запись...");
                    await _audioService.StartRecordingAsync();
                    IsRecording = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"DEBUG: Ошибка при начале записи: {ex.Message}");
                    Console.WriteLine($"DEBUG: Стек вызовов: {ex.StackTrace}");
                    StatusText = $"Ошибка: {ex.Message}";
                    await _dialogService.DisplayAlertAsync("Ошибка", $"Не удалось начать запись: {ex.Message}", "OK");
                }
            }
        }

        private async Task UploadAudioAsync()
        {
            try
            {
                StatusText = "Выбор аудиофайла...";
                Console.WriteLine("DEBUG: Выбираем аудиофайл...");

                _audioFilePath = await _audioService.PickAudioFileAsync();
                Console.WriteLine($"DEBUG: Выбран файл: {_audioFilePath}");

                if (string.IsNullOrEmpty(_audioFilePath))
                {
                    Console.WriteLine("DEBUG: Выбор файла отменен");
                    StatusText = "Выбор файла отменен";
                    return;
                }

                if (File.Exists(_audioFilePath))
                {
                    var fileInfo = new FileInfo(_audioFilePath);
                    Console.WriteLine($"DEBUG: Файл существует, размер: {fileInfo.Length} байт");
                }
                else
                {
                    Console.WriteLine("DEBUG: ОШИБКА - выбранный файл не существует");
                }

                StatusText = "Файл выбран";
                OnPropertyChanged(nameof(CanSave));
                ((Command)SaveRecordingCommand).ChangeCanExecute();

                Console.WriteLine($"DEBUG: CanSave после выбора файла: {CanSave}");

                // Запрашиваем название встречи
                await RequestMeetingTitleAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DEBUG: Ошибка при выборе файла: {ex.Message}");
                Console.WriteLine($"DEBUG: Стек вызовов: {ex.StackTrace}");
                StatusText = $"Ошибка: {ex.Message}";
                await _dialogService.DisplayAlertAsync("Ошибка", $"Не удалось выбрать аудиофайл: {ex.Message}", "OK");
            }
        }

        private async Task RequestMeetingTitleAsync()
        {
            Console.WriteLine("DEBUG: Запрашиваем название встречи");

            _meetingTitle = await _dialogService.DisplayPromptAsync(
                "Название встречи",
                "Введите название встречи для сохранения",
                "OK",
                "Отмена",
                "Встреча " + DateTime.Now.ToString("dd.MM.yyyy HH:mm"));

            Console.WriteLine($"DEBUG: Название встречи: {_meetingTitle}");
        }

        private async Task SaveRecordingAsync()
        {
            Console.WriteLine($"DEBUG: SaveRecordingAsync вызван, _audioFilePath={_audioFilePath}");

            if (string.IsNullOrEmpty(_audioFilePath))
            {
                Console.WriteLine("DEBUG: Путь к аудиофайлу пуст, выход из метода");
                return;
            }

            try
            {
                IsBusy = true;
                StatusText = "Сохранение встречи...";
                Console.WriteLine($"DEBUG: Начало сохранения встречи: {_meetingTitle}, аудиофайл: {_audioFilePath}");

                // Проверяем существование файла перед сохранением
                if (!File.Exists(_audioFilePath))
                {
                    Console.WriteLine($"DEBUG: ОШИБКА - аудиофайл не существует по пути: {_audioFilePath}");
                    await _dialogService.DisplayAlertAsync("Ошибка", "Аудиофайл не найден", "OK");
                    return;
                }

                // Создаем запись о встрече
                var meetingRecord = await _meetingService.CreateMeetingRecordAsync(_meetingTitle, _audioFilePath);
                Console.WriteLine($"DEBUG: Встреча создана с ID: {meetingRecord.Id}");

                // Проверяем, что запись создана
                var createdRecord = await _meetingService.GetMeetingRecordAsync(meetingRecord.Id);
                if (createdRecord != null)
                {
                    Console.WriteLine($"DEBUG: Проверка: Встреча успешно загружена из хранилища");
                }
                else
                {
                    Console.WriteLine("DEBUG: ОШИБКА: Не удалось загрузить созданную встречу из хранилища");
                }

                // Начинаем обработку в фоновом режиме
                _ = Task.Run(async () =>
                {
                    try
                    {
                        Console.WriteLine($"DEBUG: Запуск фоновой обработки аудио для встречи {meetingRecord.Id}");
                        await _meetingService.ProcessMeetingAudioAsync(meetingRecord.Id);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"DEBUG: Ошибка фоновой обработки: {ex.Message}");
                    }
                });

                // Переходим на страницу результатов
                await Shell.Current.GoToAsync($"{nameof(ResultsPage)}?id={meetingRecord.Id}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DEBUG: Ошибка при сохранении встречи: {ex.Message}");
                Console.WriteLine($"DEBUG: Стек вызовов: {ex.StackTrace}");
                StatusText = $"Ошибка: {ex.Message}";
                await _dialogService.DisplayAlertAsync("Ошибка", $"Не удалось сохранить встречу: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task CancelAsync()
        {
            Console.WriteLine("DEBUG: CancelAsync вызван");

            if (IsRecording)
            {
                Console.WriteLine("DEBUG: Отмена во время записи, запрашиваем подтверждение");

                bool confirm = await _dialogService.DisplayAlertAsync(
                    "Отменить запись?",
                    "Текущая запись будет потеряна. Вы уверены?",
                    "Да",
                    "Нет");

                if (confirm)
                {
                    try
                    {
                        Console.WriteLine("DEBUG: Останавливаем запись при отмене");
                        await _audioService.StopRecordingAsync();
                        IsRecording = false;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"DEBUG: Ошибка при остановке записи: {ex.Message}");
                        await _dialogService.DisplayAlertAsync("Ошибка", $"Не удалось остановить запись: {ex.Message}", "OK");
                    }
                }
                else
                {
                    Console.WriteLine("DEBUG: Отмена отменена пользователем");
                    return;
                }
            }

            Console.WriteLine("DEBUG: Возврат на предыдущую страницу");
            await Shell.Current.GoToAsync("..");
        }

        private void OnRecordingDurationChanged(object sender, double duration)
        {
            _timerValue = duration;

            // Обновляем отображение таймера
            TimeSpan time = TimeSpan.FromSeconds(duration);
            TimerText = time.ToString(@"mm\:ss");

            // Проверяем ограничение длительности (10 минут)
            if (duration >= 600 && IsRecording)
            {
                Console.WriteLine("DEBUG: Достигнут предел длительности записи (10 минут)");
                // Останавливаем запись, если достигнут предел
                ToggleRecordingCommand.Execute(null);
            }
        }

        // Метод для отладки - принудительно разблокирует кнопку сохранения
        private void ForceEnableSaveButton()
        {
            Console.WriteLine("DEBUG: Принудительное разблокирование кнопки сохранения");

            // Создаем временный файл для тестирования
            string tempPath = Path.Combine(FileSystem.CacheDirectory, "debug_test.wav");
            File.WriteAllText(tempPath, "TEST");

            _audioFilePath = tempPath;
            Console.WriteLine($"DEBUG: Установлен тестовый путь к файлу: {_audioFilePath}");

            OnPropertyChanged(nameof(CanSave));
            ((Command)SaveRecordingCommand).ChangeCanExecute();

            Console.WriteLine($"DEBUG: CanSave после принудительной разблокировки: {CanSave}");
        }
    }
}