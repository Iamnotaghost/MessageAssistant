// Models/SpeakerInfo.cs
using System;
using System.Drawing;

namespace MobileAssistant.Models
{
    public class SpeakerInfo
    {
        /// <summary>
        /// Идентификатор говорящего
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Имя говорящего, заданное пользователем
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Цвет для выделения этого говорящего в интерфейсе
        /// </summary>
        public string ColorHex { get; set; }

        /// <summary>
        /// Идентификатор встречи, к которой относится говорящий
        /// </summary>
        public string MeetingId { get; set; }
    }
}