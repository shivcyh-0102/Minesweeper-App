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

        public int Row { get; set; }
        public int Column { get; set; }
        public bool IsMine { get; set; }

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
                var color = !IsRevealed ? Color.FromArgb("#4CAF50") :
                    IsMine ? Color.FromArgb("#FF4444") :
                    Color.FromArgb("#E8F5E9");
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