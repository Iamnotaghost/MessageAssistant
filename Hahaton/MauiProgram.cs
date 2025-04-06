// Изменения в MauiProgram.cs для интеграции с Нейрошлюзом Ростелеком
using MobileAssistant.Data.Database;
using MobileAssistant.Models;
using MobileAssistant.Services.Implementation;
using MobileAssistant.Services.Interfaces;
using MobileAssistant.ViewModels;
using MobileAssistant.Views;
using Microsoft.Extensions.Logging;
using Plugin.Maui.Audio;
using System.IO;
using CommunityToolkit.Maui;
using MobileAssistant.Data.Repository;

namespace MobileAssistant
{
    public static class MauiProgram
    {
        // Обновленный MauiProgram.cs для YandexGPT и SpeechKit
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Настройка базы данных
            string dbPath = Path.Combine(FileSystem.AppDataDirectory, "meetingassistant.db3");
            Console.WriteLine($"DEBUG: Путь к базе данных: {dbPath}");
            builder.Services.AddSingleton(s => new AppDbContext(dbPath));

            // Настройка для аудио
            builder.Services.AddSingleton(AudioManager.Current);

            // Регистрация сервисов
            builder.Services.AddSingleton<IAudioService, AudioService>();
            builder.Services.AddSingleton<ISpeechRecognitionService, YandexSpeechRecognitionService>(); // Yandex SpeechKit
            builder.Services.AddSingleton<ITextProcessingService, YandexGptServices>(); // YandexGPT
            builder.Services.AddSingleton<IMeetingService, MeetingService>();
            builder.Services.AddSingleton<IEmailService, EmailService>();
            builder.Services.AddSingleton<IDialogService, DialogService>();

            // Регистрация репозиториев
            builder.Services.AddSingleton<IRepository<MeetingRecord>, MeetingRepository>();

            // Регистрация HttpClient
            builder.Services.AddHttpClient("YandexApi", client =>
            {
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            });
            builder.Services.AddHttpClient("RtcApi", client =>
            {
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            });

            // Регистрация настроек приложения с ключами для Yandex API
            builder.Services.AddSingleton(new AppSettings
            {
                YandexApiKey = "AQVNz_Ql-m4xU7XJr7JFPyKIrDFxlj7e4ifjokt1", // Ваш секретный ключ
                YandexKeyId = "ajefugivkrhhaeqsjbti", // Идентификатор ключа
                YandexFolderId = "b1g5q6olfa7srdkn6e0e", // ID каталога, если есть

                GigaChatApiToken = "eyJhbGciOiJIUzM4NCJ9.eyJzY29wZXMiOlsiZ2lnYUNoYXQiXSwic3ViIjoiaGs5IiwiaWF0IjoxNzQzMDAzMTg5LCJleHAiOjE3NDQyMTI3ODl9.2a8SkaENb_oBF_hVIAGx2H5xNRx3ppPAlt0PcZBOdywoK1Ue7_eGtZHEFeaaNR_q",
                GigaChatApiEndpoint = "https://ai.rt.ru/api/1.0/gigachat/chat",

                MaxRecordingDurationSeconds = 600, // 10 минут
                AudioFormat = "wav",
                AudioBitrate = 320000,

                EmailConfiguration = new SmtpSettings
                {
                    Server = "smtp.gmail.com",
                    Port = 587,
                    UseSsl = true,
                    Username = "mrmiksert@gmail.com",
                    Password = "160777261272Sda!", // Используйте app password
                    SenderEmail = "mrmiksert@gmail.com",
                    SenderName = "Meeting Assistant"
                }
            });

            // Регистрация ViewModels
            builder.Services.AddTransient<MainViewModel>();
            builder.Services.AddTransient<RecordingViewModel>();
            builder.Services.AddTransient<ResultsViewModel>();
            builder.Services.AddTransient<HistoryViewModel>();
            builder.Services.AddTransient<SpeakerSettingsViewModel>();

            // Регистрация Views
            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<RecordingPage>();
            builder.Services.AddTransient<ResultsPage>();
            builder.Services.AddTransient<HistoryPage>();
            builder.Services.AddTransient<SpeakerSettingsPage>();

            // Логирование в Debug-режиме
#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}