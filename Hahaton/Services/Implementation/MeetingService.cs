using MobileAssistant.Data.Repository;
using MobileAssistant.Models;
using MobileAssistant.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MobileAssistant.Services.Implementation
{
    public class MeetingService : IMeetingService
    {
        private readonly ISpeechRecognitionService _speechRecognitionService;
        private readonly ITextProcessingService _textProcessingService;
        private readonly IAudioService _audioService;
        private readonly IRepository<MeetingRecord> _meetingRepository;

        public event EventHandler<(string MeetingId, MeetingProcessingStatus Status, int Progress)> ProcessingStatusChanged;

        public MeetingService(
            ISpeechRecognitionService speechRecognitionService,
            ITextProcessingService textProcessingService,
            IAudioService audioService,
            IRepository<MeetingRecord> meetingRepository)
        {
            _speechRecognitionService = speechRecognitionService;
            _textProcessingService = textProcessingService;
            _audioService = audioService;
            _meetingRepository = meetingRepository;

            // Подписываемся на события прогресса транскрипции
            _speechRecognitionService.TranscriptionProgressChanged += OnTranscriptionProgressChanged;
        }

        public async Task<MeetingRecord> CreateMeetingRecordAsync(string title, string audioFilePath)
        {
            if (string.IsNullOrEmpty(audioFilePath))
                throw new ArgumentException("Audio file path cannot be empty", nameof(audioFilePath));

            // Создаем запись о встрече
            var meetingRecord = new MeetingRecord
            {
                Title = string.IsNullOrEmpty(title) ? $"Встреча {DateTime.Now:g}" : title,
                AudioFilePath = audioFilePath,
                RecordedAt = DateTime.Now,
                Status = MeetingProcessingStatus.New
            };

            // Получаем длительность аудио
            try
            {
                meetingRecord.Duration = await _audioService.GetAudioDurationAsync(audioFilePath);
            }
            catch (Exception ex)
            {
                // Запишем ошибку, но продолжим создание записи
                meetingRecord.ErrorMessage = $"Ошибка определения длительности: {ex.Message}";
            }

            // Сохраняем в репозиторий
            await _meetingRepository.AddAsync(meetingRecord);

            return meetingRecord;
        }

        public async Task<MeetingRecord> GetMeetingRecordAsync(string id)
        {
            return await _meetingRepository.GetByIdAsync(id);
        }

        public async Task<List<MeetingRecord>> GetAllMeetingRecordsAsync()
        {
            return await _meetingRepository.GetAllAsync();
        }

        public async Task UpdateMeetingRecordAsync(MeetingRecord meetingRecord)
        {
            await _meetingRepository.UpdateAsync(meetingRecord);
        }

        public async Task DeleteMeetingRecordAsync(string id)
        {
            await _meetingRepository.DeleteAsync(id);
        }

        public async Task<MeetingRecord> ProcessMeetingAudioAsync(string meetingId)
        {
            var meetingRecord = await _meetingRepository.GetByIdAsync(meetingId);

            if (meetingRecord == null)
                throw new ArgumentException($"Meeting record with ID {meetingId} not found", nameof(meetingId));

            try
            {
                // Обновляем статус
                meetingRecord.Status = MeetingProcessingStatus.Transcribing;
                await UpdateMeetingRecordAsync(meetingRecord);

                // Уведомляем о смене статуса
                NotifyStatusChanged(meetingId, MeetingProcessingStatus.Transcribing, 0);

                // Выполняем транскрипцию с разделением по ролям
                var transcriptionResult = await _speechRecognitionService.TranscribeAudioWithDiarizationAsync(meetingRecord.AudioFilePath);
                meetingRecord.Transcription = transcriptionResult;

                // Обновляем статус
                meetingRecord.Status = MeetingProcessingStatus.Summarizing;
                await UpdateMeetingRecordAsync(meetingRecord);

                // Уведомляем о смене статуса
                NotifyStatusChanged(meetingId, MeetingProcessingStatus.Summarizing, 50);

                // Выполняем суммаризацию
                var summaryResult = await _textProcessingService.SummarizeTranscriptionAsync(transcriptionResult);
                meetingRecord.Summary = summaryResult;

                // Обновляем статус
                meetingRecord.Status = MeetingProcessingStatus.ExtractingCommitments;
                await UpdateMeetingRecordAsync(meetingRecord);

                // Уведомляем о смене статуса
                NotifyStatusChanged(meetingId, MeetingProcessingStatus.ExtractingCommitments, 75);

                // Выделяем обязательства
                var commitments = await _textProcessingService.ExtractCommitmentsAsync(transcriptionResult);
                meetingRecord.Commitments = commitments;

                // Определяем говорящих
                var speakers = await _textProcessingService.IdentifySpeakersAsync(transcriptionResult);
                meetingRecord.Speakers = speakers;

                // Обновляем статус
                meetingRecord.Status = MeetingProcessingStatus.Completed;
                await UpdateMeetingRecordAsync(meetingRecord);

                // Уведомляем о смене статуса
                NotifyStatusChanged(meetingId, MeetingProcessingStatus.Completed, 100);

                return meetingRecord;
            }
            catch (Exception ex)
            {
                // В случае ошибки обновляем статус и сохраняем сообщение об ошибке
                meetingRecord.Status = MeetingProcessingStatus.Failed;
                meetingRecord.ErrorMessage = ex.Message;
                await UpdateMeetingRecordAsync(meetingRecord);

                // Уведомляем о смене статуса
                NotifyStatusChanged(meetingId, MeetingProcessingStatus.Failed, 0);

                throw;
            }
        }

        public async Task UpdateSpeakerNamesAsync(string meetingId, Dictionary<int, string> speakerNames)
        {
            var meetingRecord = await _meetingRepository.GetByIdAsync(meetingId);

            if (meetingRecord == null)
                throw new ArgumentException($"Meeting record with ID {meetingId} not found", nameof(meetingId));

            // Обновляем имена говорящих
            foreach (var speaker in meetingRecord.Speakers)
            {
                if (speakerNames.TryGetValue(speaker.Id, out var name))
                {
                    speaker.Name = name;
                }
            }

            // Обновляем имена говорящих в сегментах транскрипции
            foreach (var segment in meetingRecord.Transcription.Segments)
            {
                if (speakerNames.TryGetValue(segment.SpeakerId, out var name))
                {
                    segment.SpeakerName = name;
                }
            }

            await UpdateMeetingRecordAsync(meetingRecord);
        }

        private void NotifyStatusChanged(string meetingId, MeetingProcessingStatus status, int progress)
        {
            ProcessingStatusChanged?.Invoke(this, (meetingId, status, progress));
        }

        private void OnTranscriptionProgressChanged(object sender, int progress)
        {
            // Можно использовать для обновления прогресса транскрипции
            // Но нужно как-то связать с конкретной встречей
        }
    }
}