using MobileAssistant.Models;
using MobileAssistant.Services.Interfaces;
using MobileAssistant.Views;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MobileAssistant.ViewModels
{
    [QueryProperty(nameof(MeetingId), "id")]
    public class SpeakerSettingsViewModel : BaseViewModel
    {
        private readonly IMeetingService _meetingService;
        private readonly IDialogService _dialogService;

        private string _meetingId;
        private MeetingRecord _meeting;

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

        public ObservableCollection<SpeakerViewModel> Speakers { get; } = new ObservableCollection<SpeakerViewModel>();

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public SpeakerSettingsViewModel(IMeetingService meetingService, IDialogService dialogService)
        {
            _meetingService = meetingService;
            _dialogService = dialogService;

            Title = "Настройка говорящих";

            // Команды
            SaveCommand = new Command(async () => await SaveSpeakersAsync());
            CancelCommand = new Command(async () => await Shell.Current.GoToAsync(".."));
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

                // Заполняем коллекцию говорящих
                Speakers.Clear();
                foreach (var speaker in Meeting.Speakers)
                {
                    Speakers.Add(new SpeakerViewModel
                    {
                        Id = speaker.Id,
                        Name = speaker.Name,
                        ColorHex = speaker.ColorHex
                    });
                }
            }
            catch (Exception ex)
            {
                await _dialogService.DisplayAlertAsync("Ошибка", $"Не удалось загрузить данные говорящих: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task SaveSpeakersAsync()
        {
            try
            {
                IsBusy = true;

                // Создаем словарь с обновленными именами говорящих
                var speakerNames = new Dictionary<int, string>();

                foreach (var speaker in Speakers)
                {
                    speakerNames[speaker.Id] = speaker.Name;
                }

                // Обновляем имена говорящих
                await _meetingService.UpdateSpeakerNamesAsync(MeetingId, speakerNames);

                await _dialogService.DisplayToastAsync("Имена говорящих сохранены");

                // Возвращаемся на предыдущую страницу
                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                await _dialogService.DisplayAlertAsync("Ошибка", $"Не удалось сохранить имена говорящих: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }

    public class SpeakerViewModel : INotifyPropertyChanged
    {
        private int _id;
        private string _name;
        private string _colorHex;

        public int Id
        {
            get => _id;
            set
            {
                if (_id != value)
                {
                    _id = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Id)));
                }
            }
        }

        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
                }
            }
        }

        public string ColorHex
        {
            get => _colorHex;
            set
            {
                if (_colorHex != value)
                {
                    _colorHex = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ColorHex)));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}