using Avalonia.Media;

namespace ChatApp.Avalonia.ViewModels
{
    public class MessageViewModel : ViewModelBase
    {
        /// <summary>
        /// An array of colors used to assign a unique color to each user based on their username hash code.
        /// </summary>
        private static readonly Color[] UserColors =
        [
            Color.FromRgb(65, 105, 225),    // RoyalBlue
            Color.FromRgb(34, 139, 34),     // ForestGreen
            Color.FromRgb(220, 20, 60),     // Crimson
            Color.FromRgb(153, 50, 204),    // DarkOrchid
            Color.FromRgb(0, 128, 128),     // Teal
            Color.FromRgb(210, 105, 30),    // Chocolate
            Color.FromRgb(70, 130, 180),    // SteelBlue
            Color.FromRgb(205, 92, 92),     // IndianRed
        ];
        /// <summary>
        /// Gets or sets the timestamp of the message in string format.
        /// </summary>
        public string Timestamp { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the username of the sender of the message. If the username is "System", it indicates that the message is a system message.
        /// </summary>
        public string Username { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the content of the message.
        /// </summary>
        public string Content { get; set; } = string.Empty;
        /// <summary>
        /// Gets a value indicating whether the message is a system message.
        /// </summary>
        public bool IsSystem => Username == "System";
        /// <summary>
        /// Gets the color associated with the user based on their username hash code. System messages are displayed in gray.
        /// </summary>
        public IBrush UserColor
        {
            get
            {
                if (IsSystem) return Brushes.Gray;
                var hash = Math.Abs(Username.GetHashCode());
                return new SolidColorBrush(UserColors[hash % UserColors.Length]);
            }
        }
    }
}
