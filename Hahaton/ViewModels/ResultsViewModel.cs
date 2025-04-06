using MobileAssistant.Models;
using MobileAssistant.Views;
using MobileAssistant.Services.Interfaces;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MobileAssistant.ViewModels
{
    [QueryProperty(nameof(MeetingId), "id")]
    public class ResultsViewModel : BaseViewModel
    {
        private readonly IMeetingService _meetingService;
        private readonly IEmailService _emailService;
        private readonly IDialogService _dialogService;

        private string _meetingId;
        private MeetingRecord _meeting;
        private string _statusText;
        private int _processingProgress;
        private bool _isProcessing;
        private int _currentTabIndex;

        public int CurrentTabIndex
        {
            get => _currentTabIndex;
            set => SetProperty(ref _currentTabIndex, value);
        }

        public string MeetingId
        {
            get => _meetingId;
            set
            {
                if (SetProperty(ref _meetingId, value))
                {
                    LoadMeetingAsync().ConfigureAwait(false);
                }
            }
        }

        public MeetingRecord Meeting
        {
            get => _meeting;
            set => SetProperty(ref _meeting, value);
        }

        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        public int ProcessingProgress
        {
            get => _processingProgress;
            set => SetProperty(ref _processingProgress, value);
        }

        public bool IsProcessing
        {
            get => _isProcessing;
            set => SetProperty(ref _isProcessing, value);
        }

        public ObservableCollection<TabItem> Tabs { get; } = new ObservableCollection<TabItem>();

        public ICommand SendEmailCommand { get; }
        public ICommand EditSpeakersCommand { get; }
        public ICommand BackCommand { get; }

        public ResultsViewModel(IMeetingService meetingService, IEmailService emailService, IDialogService dialogService)
        {
            _meetingService = meetingService;
            _emailService = emailService;
            _dialogService = dialogService;

            Title = "Результаты обработки";

            // Инициализация вкладок
            Tabs.Add(new TabItem { Title = "Расшифровка", IsSelected = true });
            Tabs.Add(new TabItem { Title = "Краткий пересказ", IsSelected = false });
            Tabs.Add(new TabItem { Title = "Обязательства", IsSelected = false });

            // Команды
            SendEmailCommand = new Command(async () => await SendEmailAsync(), () => Meeting?.Status == MeetingProcessingStatus.Completed);
            EditSpeakersCommand = new Command(async () => await EditSpeakersAsync(), () => Meeting?.Status == MeetingProcessingStatus.Completed);
            BackCommand = new Command(async () => await Shell.Current.GoToAsync(".."));

            // Подписываемся на изменение статуса обработки
            _meetingService.ProcessingStatusChanged += OnProcessingStatusChanged;
        }

        private async Task LoadMeetingAsync()
        {
            if (string.IsNullOrEmpty(MeetingId))
                return;

            try
            {
                IsBusy = true;

                // Загружаем запись встречи
                Meeting = await _meetingService.GetMeetingRecordAsync(MeetingId);

                if (Meeting == null)
                {
                    await _dialogService.DisplayAlertAsync("Ошибка", "Запись встречи не найдена", "OK");
                    await Shell.Current.GoToAsync("..");
                    return;
                }

                // Обновляем заголовок
                Title = Meeting.Title;

                // Обновляем статус обработки
                UpdateProcessingStatus(Meeting.Status);
            }
            catch (Exception ex)
            {
                await _dialogService.DisplayAlertAsync("Ошибка", $"Не удалось загрузить данные встречи: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void UpdateProcessingStatus(MeetingProcessingStatus status)
        {
            IsProcessing = status != MeetingProcessingStatus.Completed && status != MeetingProcessingStatus.Failed;

            switch (status)
            {
                case MeetingProcessingStatus.New:
                    StatusText = "Ожидание обработки...";
                    ProcessingProgress = 0;
                    break;
                case MeetingProcessingStatus.Transcribing:
                    StatusText = "Идет распознавание речи...";
                    ProcessingProgress = 25;
                    break;
                case MeetingProcessingStatus.Summarizing:
                    StatusText = "Создание краткого пересказа...";
                    ProcessingProgress = 50;
                    break;
                case MeetingProcessingStatus.ExtractingCommitments:
                    StatusText = "Выделение обязательств...";
                    ProcessingProgress = 75;
                    break;
                case MeetingProcessingStatus.Completed:
                    StatusText = "Обработка завершена";
                    ProcessingProgress = 100;
                    break;
                case MeetingProcessingStatus.Failed:
                    StatusText = $"Ошибка обработки: {Meeting?.ErrorMessage}";
                    ProcessingProgress = 0;
                    break;
            }

            // Обновляем состояние команд
            ((Command)SendEmailCommand).ChangeCanExecute();
            ((Command)EditSpeakersCommand).ChangeCanExecute();
        }

        private void OnProcessingStatusChanged(object sender, (string MeetingId, MeetingProcessingStatus Status, int Progress) e)
        {
            // Проверяем, относится ли событие к текущей встрече
            if (e.MeetingId == MeetingId)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    ProcessingProgress = e.Progress;

                    if (Meeting != null)
                    {
                        Meeting.Status = e.Status;
                        UpdateProcessingStatus(e.Status);

                        // Если обработка завершена, перезагружаем данные
                        if (e.Status == MeetingProcessingStatus.Completed)
                        {
                            LoadMeetingAsync().ConfigureAwait(false);
                        }
                    }
                });
            }
        }

        private async Task SendEmailAsync()
        {
            if (Meeting?.Status != MeetingProcessingStatus.Completed)
                return;

            try
            {
                // Показываем диалог настроек email
                var emailSettings = new EmailSettings();

                // В реальном приложении здесь должен быть вызов диалога настроек email
                var email = await _dialogService.DisplayPromptAsync(
                    "Отправка результатов",
                    "Введите адрес электронной почты для отправки результатов",
                    "Отправить",
                    "Отмена",
                    "example@example.com",
                    keyboard: Keyboard.Email);

                if (string.IsNullOrEmpty(email))
                    return;

                emailSettings.RecipientEmail = email;

                // Отправляем результаты по email
                IsBusy = true;
                StatusText = "Отправка результатов...";

                bool success = await _emailService.SendMeetingResultsAsync(Meeting, emailSettings);

                if (success)
                {
                    await _dialogService.DisplayAlertAsync("Успешно", "Результаты отправлены на указанный адрес", "OK");
                }
                else
                {
                    await _dialogService.DisplayAlertAsync("Ошибка", "Не удалось отправить результаты", "OK");
                }
            }
            catch (Exception ex)
            {
                await _dialogService.DisplayAlertAsync("Ошибка", $"Не удалось отправить результаты: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
                StatusText = "Обработка завершена";
            }
        }

        private async Task EditSpeakersAsync()
        {
            await Shell.Current.GoToAsync($"{nameof(SpeakerSettingsPage)}?id={MeetingId}");
        }
    }

    public class TabItem : INotifyPropertyChanged
    {
        private string _title;
        private bool _isSelected;

        public string Title
        {
            get => _title;
            set
            {
                if (_title != value)
                {
                    _title = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Title)));
                }
            }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}