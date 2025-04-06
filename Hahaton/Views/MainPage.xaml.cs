// Views/MainPage.xaml.cs
using MobileAssistant.ViewModels;

namespace MobileAssistant.Views
{
    public partial class MainPage : ContentPage
    {
        private MainViewModel _viewModel;

        // Добавьте конструктор без параметров
        public MainPage()
        {
            InitializeComponent();
            // Получить ViewModel из DI вручную
            _viewModel = App.Current.Handler.MauiContext.Services.GetService<MainViewModel>();
            BindingContext = _viewModel;
        }

        // Оставьте существующий конструктор с инъекцией зависимостей
        public MainPage(MainViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;
        }
    }
}