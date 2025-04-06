using MobileAssistant.ViewModels;

namespace MobileAssistant.Views
{
    public partial class SpeakerSettingsPage : ContentPage
    {
        private readonly SpeakerSettingsViewModel _viewModel;

        public SpeakerSettingsPage(SpeakerSettingsViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;
        }
    }
}