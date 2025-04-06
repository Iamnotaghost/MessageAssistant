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

            Console.WriteLine("DEBUG: RecordingPage �������");
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            Console.WriteLine("DEBUG: RecordingPage.OnAppearing");

            // ������� ���������� � ����������� ��� �����������
            var appDir = FileSystem.AppDataDirectory;
            Console.WriteLine($"DEBUG: AppDataDirectory: {appDir}");

            var cacheDir = FileSystem.CacheDirectory;
            Console.WriteLine($"DEBUG: CacheDirectory: {cacheDir}");

            var meetingsDir = Path.Combine(appDir, "meetings");
            if (Directory.Exists(meetingsDir))
            {
                Console.WriteLine($"DEBUG: ���������� meetings ����������: {meetingsDir}");
                try
                {
                    var files = Directory.GetFiles(meetingsDir);
                    Console.WriteLine($"DEBUG: ���������� ������ � ����������: {files.Length}");

                    foreach (var file in files.Take(5))
                    {
                        var info = new FileInfo(file);
                        Console.WriteLine($"DEBUG: ����: {info.Name}, ������: {info.Length} ����");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"DEBUG: ������ ��� ������������ ����������: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine($"DEBUG: ���������� meetings �� ����������");
            }

            await _viewModel.OnAppearingAsync();
        }
    }
}