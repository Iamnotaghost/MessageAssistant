using MobileAssistant.Models;
using MobileAssistant.Services.Interfaces;
using MobileAssistant.Views;
using System.Windows.Input;

namespace MobileAssistant.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly IMeetingService _meetingService;

        public ICommand StartRecordingCommand { get; }
        public ICommand UploadAudioCommand { get; }
        public ICommand ViewHistoryCommand { get; }

        public MainViewModel(IMeetingService meetingService)
        {
            _meetingService = meetingService;
            Title = "Мобильный ассистент";

            StartRecordingCommand = new Command(async () => await StartRecordingAsync());
            UploadAudioCommand = new Command(async () => await UploadAudioAsync());
            ViewHistoryCommand = new Command(async () => await ViewHistoryAsync());
        }

        private async Task StartRecordingAsync()
        {
            await Shell.Current.GoToAsync(nameof(RecordingPage));
        }

        private async Task UploadAudioAsync()
        {
            await Shell.Current.GoToAsync($"{nameof(RecordingPage)}?mode=upload");
        }

        private async Task ViewHistoryAsync()
        {
            await Shell.Current.GoToAsync(nameof(HistoryPage));
        }
    }
}