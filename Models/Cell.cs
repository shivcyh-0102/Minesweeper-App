using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MinesweeperApp.Models
{
    public class GameCell : INotifyPropertyChanged
    {
        private bool _isRevealed;
        private bool _isFlagged;
        private string? _displayTextCache;
        private Color _backgroundColorCache = Colors.Gray;
        private Color _textColorCache = Colors.Black;
        private bool _displayTextCached;
        private bool _backgroundColorCached;
        private bool _textColorCached;
        private string _theme = "Classic";

        public int Row { get; set; }
        public int Column { get; set; }
        public bool IsMine { get; set; }

        public string Theme
        {
            get => _theme;
            set
            {
                if (_theme == value) return;
                _theme = value;
                _backgroundColorCached = false;
                _textColorCached = false;
                OnPropertyChanged(nameof(BackgroundColor));
                OnPropertyChanged(nameof(TextColor));
            }
        }

        public bool IsRevealed
        {
            get => _isRevealed;
            set
            {
                if (_isRevealed == value) return;
                _isRevealed = value;
                _displayTextCached = false;
                _backgroundColorCached = false;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayText));
                OnPropertyChanged(nameof(BackgroundColor));
            }
        }

        public bool IsFlagged
        {
            get => _isFlagged;
            set
            {
                if (_isFlagged == value) return;
                _isFlagged = value;
                _displayTextCached = false;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayText));
            }
        }

        public int AdjacentMines { get; set; }

        public string DisplayText
        {
            get
            {
                if (_displayTextCached) return _displayTextCache ?? "";
                _displayTextCache = IsFlagged ? "🚩" :
                    !IsRevealed ? "" :
                    IsMine ? "💣" :
                    AdjacentMines > 0 ? AdjacentMines.ToString() : "";
                _displayTextCached = true;
                return _displayTextCache;
            }
        }

        public Color BackgroundColor
        {
            get
            {
                if (_backgroundColorCached) return _backgroundColorCache;
                var color = Theme switch
                {
                    "Light" => !IsRevealed ? Color.FromArgb("#2E7D32") : IsMine ? Color.FromArgb("#D32F2F") : Color.FromArgb("#F7FBF7"),
                    "Retro" => !IsRevealed ? Color.FromArgb("#5C6B2F") : IsMine ? Color.FromArgb("#B71C1C") : Color.FromArgb("#D7C58A"),
                    "Neon" => !IsRevealed ? Color.FromArgb("#00A878") : IsMine ? Color.FromArgb("#FF2E63") : Color.FromArgb("#E9FFF8"),
                    _ => !IsRevealed ? Color.FromArgb("#4CAF50") : IsMine ? Color.FromArgb("#FF4444") : Color.FromArgb("#E8F5E9")
                };
                _backgroundColorCache = color;
                _backgroundColorCached = true;
                return color;
            }
        }

        public Color TextColor
        {
            get
            {
                if (_textColorCached) return _textColorCache;
                var color = AdjacentMines switch
                {
                    1 => Colors.Blue,
                    2 => Colors.Green,
                    3 => Colors.Red,
                    4 => Colors.DarkBlue,
                    5 => Colors.DarkRed,
                    _ => Colors.Black
                };
                _textColorCache = color;
                _textColorCached = true;
                return color;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
