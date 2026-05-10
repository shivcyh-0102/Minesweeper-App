using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MinesweeperApp.Models
{
    public class GameCell : INotifyPropertyChanged
    {
        private bool _isRevealed;
        private bool _isFlagged;

        public int Row { get; set; }
        public int Column { get; set; }
        public bool IsMine { get; set; }

        public bool IsRevealed
        {
            get => _isRevealed;
            set { _isRevealed = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayText)); OnPropertyChanged(nameof(BackgroundColor)); }
        }

        public bool IsFlagged
        {
            get => _isFlagged;
            set { _isFlagged = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayText)); }
        }

        public int AdjacentMines { get; set; }

        public string DisplayText =>
            IsFlagged ? "🚩" :
            !IsRevealed ? "" :
            IsMine ? "💣" :
            AdjacentMines > 0 ? AdjacentMines.ToString() : "";

        public Color BackgroundColor =>
            !IsRevealed ? Color.FromArgb("#4CAF50") :
            IsMine ? Color.FromArgb("#FF4444") :
            Color.FromArgb("#E8F5E9");

        public Color TextColor => AdjacentMines switch
        {
            1 => Colors.Blue,
            2 => Colors.Green,
            3 => Colors.Red,
            4 => Colors.DarkBlue,
            5 => Colors.DarkRed,
            _ => Colors.Black
        };

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}