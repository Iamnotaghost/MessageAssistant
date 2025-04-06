using SQLite;
using System.Threading.Tasks;

namespace MobileAssistant.Data.Database
{
    public class AppDbContext
    {
        private readonly SQLiteAsyncConnection _database;

        public SQLiteAsyncConnection Database => _database;

        public AppDbContext(string dbPath)
        {
            _database = new SQLiteAsyncConnection(dbPath);
            Initialize();
        }

        private async void Initialize()
        {
            try
            {
                // Создаем таблицы в базе данных
                await _database.CreateTableAsync<MeetingInfo>();
                Console.WriteLine("Database tables created successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing database: {ex.Message}");
            }
        }
    }

    // Класс для хранения базовой информации в SQLite
    [Table("MeetingInfos")]
    public class MeetingInfo
    {
        [PrimaryKey]
        public string Id { get; set; }

        public string Title { get; set; }

        public DateTime RecordedAt { get; set; }

        public double Duration { get; set; }

        public string AudioFilePath { get; set; }

        public int Status { get; set; } // Используем int вместо enum для SQLite
    }
}