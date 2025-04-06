using MobileAssistant.ViewModels;

namespace MobileAssistant.Views
{
    public partial class ResultsPage : ContentPage
    {
        private readonly ResultsViewModel _viewModel;
        private readonly TranscriptionTabView _transcriptionTabView;
        private readonly SummaryTabView _summaryTabView;
        private readonly CommitmentsTabView _commitmentsTabView;

        public ResultsPage(ResultsViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;

            // —оздаем представлени€ дл€ вкладок
            _transcriptionTabView = new TranscriptionTabView { BindingContext = _viewModel };
            _summaryTabView = new SummaryTabView { BindingContext = _viewModel };
            _commitmentsTabView = new CommitmentsTabView { BindingContext = _viewModel };

            // ѕо умолчанию показываем первую вкладку
            ShowTranscriptionTab();
        }

        private void OnTranscriptionTabClicked(object sender, EventArgs e)
        {
            ShowTranscriptionTab();
        }

        private void OnSummaryTabClicked(object sender, EventArgs e)
        {
            ShowSummaryTab();
        }

        private void OnCommitmentsTabClicked(object sender, EventArgs e)
        {
            ShowCommitmentsTab();
        }

        private void ShowTranscriptionTab()
        {
            tabContentContainer.Content = _transcriptionTabView;
            _viewModel.CurrentTabIndex = 0;
        }

        private void ShowSummaryTab()
        {
            tabContentContainer.Content = _summaryTabView;
            _viewModel.CurrentTabIndex = 1;
        }

        private void ShowCommitmentsTab()
        {
            tabContentContainer.Content = _commitmentsTabView;
            _viewModel.CurrentTabIndex = 2;
        }
    }
}