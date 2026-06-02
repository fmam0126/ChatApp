using Avalonia.Input;
using Classic.Avalonia.Theme;
using ChatApp.Avalonia.ViewModels;

namespace ChatApp.Avalonia
{
    public partial class MainWindow : ClassicWindow
    {
        private readonly MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainViewModel();
            DataContext = _viewModel;

            _viewModel.Messages.CollectionChanged += (_, _) =>
            {
                if (_viewModel.Messages.Count > 0)
                    MessagesListBox.ScrollIntoView(_viewModel.Messages.Count - 1);
            };
        }

        private void MessageInput_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && _viewModel.IsConnected)
            {
                _viewModel.SendMessageCommand.Execute(null);
                e.Handled = true;
            }
        }
    }
}
