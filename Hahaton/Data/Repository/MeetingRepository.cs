using MobileAssistant.Data.Database;
using MobileAssistant.Data.Repository;
using MobileAssistant.Models;
using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace MobileAssistant.Data.Repository
{
    public class MeetingRepository : IRepository<MeetingRecord>
    {
        private readonly SQLiteAsyncConnection _database;
        private readonly string _dataPath;

        public MeetingRepository(AppDbContext dbContext)
        {
            _database = dbContext.Database;
            _dataPath = Path.Combine(FileSystem.AppDataDirectory, "meetings");

            Console.WriteLine($"Repository initialized with path: {_dataPath}");

            // Создаем директорию для хранения данных, если она не существует
            if (!Directory.Exists(_dataPath))
            {
                Directory.CreateDirectory(_dataPath);
                Console.WriteLine("Created meetings directory");
            }
        }

        public async Task<MeetingRecord> GetByIdAsync(string id)
        {
            try
            {
                Console.WriteLine($"Getting meeting with ID: {id}");

                // Получаем базовую информацию из БД
                var meetingInfo = await _database.Table<MeetingInfo>().FirstOrDefaultAsync(m => m.Id == id);

                if (meetingInfo == null)
                {
                    Console.WriteLine($"Meeting with ID {id} not found in database");
                    return null;
                }

                // Загружаем полные данные из файла
                return await LoadMeetingRecordFromFileAsync(id);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting meeting by ID: {ex.Message}");
                return null;
            }
        }

        public async Task<List<MeetingRecord>> GetAllAsync()
        {
            try
            {
                Console.WriteLine("Getting all meetings");

                // Получаем список всех записей из БД
                var meetingInfos = await _database.Table<MeetingInfo>().ToListAsync();
                Console.WriteLine($"Found {meetingInfos.Count} meetings in database");

                var meetings = new List<MeetingRecord>();

                foreach (var info in meetingInfos)
                {
                    var meeting = await LoadMeetingRecordFromFileAsync(info.Id);
                    if (meeting != null)
                    {
                        meetings.Add(meeting);
                    }
                }

                return meetings.OrderByDescending(m => m.RecordedAt).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting all meetings: {ex.Message}");
                return new List<MeetingRecord>();
            }
        }

        public async Task AddAsync(MeetingRecord entity)
        {
            try
            {
                Console.WriteLine($"Adding new meeting: {entity.Title}");

                if (string.IsNullOrEmpty(entity.Id))
                {
                    entity.Id = Guid.NewGuid().ToString();
                    Console.WriteLine($"Generated new ID: {entity.Id}");
                }

                // Сохраняем базовую информацию в БД
                var meetingInfo = new MeetingInfo
                {
                    Id = entity.Id,
                    Title = entity.Title,
                    RecordedAt = entity.RecordedAt,
                    Duration = entity.Duration,
                    AudioFilePath = entity.AudioFilePath,
                    Status = (int)entity.Status
                };

                var result = await _database.InsertAsync(meetingInfo);
                Console.WriteLine($"Database insert result: {result}");

                // Сохраняем полные данные в файл
                await SaveMeetingRecordToFileAsync(entity);

                Console.WriteLine("Meeting added successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding meeting: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        public async Task UpdateAsync(MeetingRecord entity)
        {
            try
            {
                Console.WriteLine($"Updating meeting: {entity.Title}");

                // Обновляем базовую информацию в БД
                var meetingInfo = new MeetingInfo
                {
                    Id = entity.Id,
                    Title = entity.Title,
                    RecordedAt = entity.RecordedAt,
                    Duration = entity.Duration,
                    AudioFilePath = entity.AudioFilePath,
                    Status = (int)entity.Status
                };

                var result = await _database.UpdateAsync(meetingInfo);
                Console.WriteLine($"Database update result: {result}");

                // Сохраняем полные данные в файл
                await SaveMeetingRecordToFileAsync(entity);

                Console.WriteLine("Meeting updated successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating meeting: {ex.Message}");
                throw;
            }
        }

        public async Task DeleteAsync(string id)
        {
            try
            {
                Console.WriteLine($"Deleting meeting with ID: {id}");

                // Удаляем запись из БД
                var result = await _database.DeleteAsync<MeetingInfo>(id);
                Console.WriteLine($"Database delete result: {result}");

                // Удаляем файл с полными данными
                var filePath = GetMeetingFilePath(id);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    Console.WriteLine("Meeting file deleted");
                }

                Console.WriteLine("Meeting deleted successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting meeting: {ex.Message}");
                throw;
            }
        }

        private async Task SaveMeetingRecordToFileAsync(MeetingRecord meeting)
        {
            try
            {
                var filePath = GetMeetingFilePath(meeting.Id);
                Console.WriteLine($"DEBUG: Сохраняем встречу в файл: {filePath}");

                // Используем Newtonsoft.Json вместо System.Text.Json
                string json = Newtonsoft.Json.JsonConvert.SerializeObject(meeting,
                    Newtonsoft.Json.Formatting.Indented,
                    new Newtonsoft.Json.JsonSerializerSettings
                    {
                        TypeNameHandling = Newtonsoft.Json.TypeNameHandling.None,
                        ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore
                    });

                await File.WriteAllTextAsync(filePath, json);

                Console.WriteLine("DEBUG: Встреча успешно сохранена в файл");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DEBUG: Ошибка при сохранении встречи в файл: {ex.Message}");
                Console.WriteLine($"DEBUG: Стек вызовов: {ex.StackTrace}");
                throw;
            }
        }

        private async Task<MeetingRecord> LoadMeetingRecordFromFileAsync(string id)
        {
            try
            {
                var filePath = GetMeetingFilePath(id);
                Console.WriteLine($"Loading meeting from file: {filePath}");

                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"Meeting file not found: {filePath}");
                    return null;
                }

                var json = await File.ReadAllTextAsync(filePath);

                var meeting = JsonSerializer.Deserialize<MeetingRecord>(json);
                Console.WriteLine($"Meeting loaded from file: {meeting.Title}");
                return meeting;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading meeting from file: {ex.Message}");
                return null;
            }
        }

        private string GetMeetingFilePath(string id)
        {
            return Path.Combine(_dataPath, $"{id}.json");
        }
    }
}