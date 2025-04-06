using System.Threading.Tasks;

namespace MobileAssistant.Services.Interfaces
{
    public interface IDialogService
    {
        Task DisplayAlertAsync(string title, string message, string cancel);
        Task<bool> DisplayAlertAsync(string title, string message, string accept, string cancel);
        Task<string> DisplayPromptAsync(string title, string message, string accept, string cancel, string placeholder = null, int maxLength = -1, Keyboard keyboard = null);
        Task DisplayToastAsync(string message, int durationMilliseconds = 2000);
    }
}