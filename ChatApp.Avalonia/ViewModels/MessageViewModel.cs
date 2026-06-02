using Avalonia.Media;

namespace ChatApp.Avalonia.ViewModels
{
    public class MessageViewModel : ViewModelBase
    {
        private static readonly Color[] UserColors =
        [
            Color.FromRgb(0, 255, 255),       // Aqua
            Color.FromRgb(127, 255, 0),       // Chartreuse
            Color.FromRgb(255, 0, 255),       // Fuchsia
            Color.FromRgb(255, 215, 0),       // Gold
            Color.FromRgb(147, 112, 219),     // MediumPurple
            Color.FromRgb(0, 191, 255),       // DeepSkyBlue
            Color.FromRgb(255, 105, 180),     // HotPink
            Color.FromRgb(255, 165, 0),       // Orange
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
