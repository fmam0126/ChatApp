using Avalonia.Media;

namespace ChatApp.Avalonia.ViewModels
{
    public class MessageViewModel : ViewModelBase
    {
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

        public string Timestamp { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public bool IsSystem => Username == "System";

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
