// Services/Implementation/DialogService.cs
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using MobileAssistant.Services.Interfaces;
using System.Threading.Tasks;

namespace MobileAssistant.Services.Implementation
{
    public class DialogService : IDialogService
    {
        public Task DisplayAlertAsync(string title, string message, string cancel)
        {
            return Application.Current.MainPage.DisplayAlert(title, message, cancel);
        }

        public Task<bool> DisplayAlertAsync(string title, string message, string accept, string cancel)
        {
            return Application.Current.MainPage.DisplayAlert(title, message, accept, cancel);
        }

        public Task<string> DisplayPromptAsync(string title, string message, string accept, string cancel, string placeholder = null, int maxLength = -1, Keyboard keyboard = null)
        {
            return Application.Current.MainPage.DisplayPromptAsync(title, message, accept, cancel, placeholder, maxLength, keyboard: keyboard);
        }

        public Task DisplayToastAsync(string message, int durationMilliseconds = 2000)
        {
            var toast = Toast.Make(message, ToastDuration.Short);
            return toast.Show();
        }
    }
}