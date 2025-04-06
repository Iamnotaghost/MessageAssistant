using MobileAssistant.ViewModels;
using System.IO;

namespace MobileAssistant.Views
{
    public partial class RecordingPage : ContentPage
    {
        private readonly RecordingViewModel _viewModel;

        public RecordingPage(RecordingViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;

            Console.WriteLine("DEBUG: RecordingPage создана");
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            Console.WriteLine("DEBUG: RecordingPage.OnAppearing");

            // Выводим информацию о директориях для диагностики
            var appDir = FileSystem.AppDataDirectory;
            Console.WriteLine($"DEBUG: AppDataDirectory: {appDir}");

            var cacheDir = FileSystem.CacheDirectory;
            Console.WriteLine($"DEBUG: CacheDirectory: {cacheDir}");

            var meetingsDir = Path.Combine(appDir, "meetings");
            if (Directory.Exists(meetingsDir))
            {
                Console.WriteLine($"DEBUG: Директория meetings существует: {meetingsDir}");
                try
                {
                    var files = Directory.GetFiles(meetingsDir);
                    Console.WriteLine($"DEBUG: Количество файлов в директории: {files.Length}");

                    foreach (var file in files.Take(5))
                    {
                        var info = new FileInfo(file);
                        Console.WriteLine($"DEBUG: Файл: {info.Name}, размер: {info.Length} байт");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"DEBUG: Ошибка при сканировании директории: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine($"DEBUG: Директория meetings не существует");
            }

            await _viewModel.OnAppearingAsync();
        }
    }
}