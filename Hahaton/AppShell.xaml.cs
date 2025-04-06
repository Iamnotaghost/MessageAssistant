using MobileAssistant.Views;

namespace MobileAssistant
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            // Регистрируем маршруты для навигации
            Routing.RegisterRoute(nameof(RecordingPage), typeof(RecordingPage));
            Routing.RegisterRoute(nameof(ResultsPage), typeof(ResultsPage));
            Routing.RegisterRoute(nameof(HistoryPage), typeof(HistoryPage));
            Routing.RegisterRoute(nameof(SpeakerSettingsPage), typeof(SpeakerSettingsPage));
        }
    }
}