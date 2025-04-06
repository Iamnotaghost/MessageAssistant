using MobileAssistant.Models;
using MobileAssistant.Services.Interfaces;
using MobileAssistant.Views;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MobileAssistant.ViewModels
{
    public class HistoryViewModel : BaseViewModel
    {
        private readonly IMeetingService _meetingService;
        private readonly IDialogService _dialogService;

        public ObservableCollection<MeetingRecord> Meetings { get; } = new ObservableCollection<MeetingRecord>();

        public ICommand RefreshCommand { get; }
        public ICommand OpenMeetingCommand { get; }
        public ICommand DeleteMeetingCommand { get; }

        public HistoryViewModel(IMeetingService meetingService, IDialogService dialogService)
        {
            _meetingService = meetingService;
            _dialogService = dialogService;

            Title = "История встреч";

            // Команды
            RefreshCommand = new Command(async () => await LoadMeetingsAsync());
            OpenMeetingCommand = new Command<string>(async (id) => await OpenMeetingAsync(id));
            DeleteMeetingCommand = new Command<string>(async (id) => await DeleteMeetingAsync(id));
        }

        public async Task OnAppearingAsync()
        {
            await LoadMeetingsAsync();
        }

        private async Task LoadMeetingsAsync()
        {
            try
            {
                IsBusy = true;

                // Очищаем текущий список
                Meetings.Clear();

                // Загружаем все записи встреч
                var meetings = await _meetingService.GetAllMeetingRecordsAsync();

                // Добавляем в ObservableCollection
                foreach (var meeting in meetings)
                {
                    Meetings.Add(meeting);
                }
            }
            catch (Exception ex)
            {
                await _dialogService.DisplayAlertAsync("Ошибка", $"Не удалось загрузить историю встреч: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task OpenMeetingAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
                return;

            await Shell.Current.GoToAsync($"{nameof(ResultsPage)}?id={id}");
        }

        private async Task DeleteMeetingAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
                return;

            bool confirm = await _dialogService.DisplayAlertAsync(
                "Удаление встречи",
                "Вы уверены, что хотите удалить эту запись встречи? Это действие нельзя отменить.",
                "Удалить",
                "Отмена");

            if (!confirm)
                return;

            try
            {
                IsBusy = true;

                // Удаляем запись о встрече
                await _meetingService.DeleteMeetingRecordAsync(id);

                // Перезагружаем список
                await LoadMeetingsAsync();

                await _dialogService.DisplayToastAsync("Запись удалена");
            }
            catch (Exception ex)
            {
                await _dialogService.DisplayAlertAsync("Ошибка", $"Не удалось удалить запись: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}