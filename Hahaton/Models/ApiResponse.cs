// Models/ApiResponse.cs
namespace MobileAssistant.Models
{
    public class ApiResponse<T>
    {
        /// <summary>
        /// Результат выполнения запроса к API
        /// </summary>
        public T Result { get; set; }

        /// <summary>
        /// Успешность выполнения запроса
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Сообщение об ошибке (если есть)
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Код ошибки (если есть)
        /// </summary>
        public string ErrorCode { get; set; }
    }
}