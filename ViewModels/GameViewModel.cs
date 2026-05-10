using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using MinesweeperApp.Models;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace MinesweeperApp.ViewModels
{
    public class GameViewModel : INotifyPropertyChanged
    {
        public const int Rows = 9;
        public const int Cols = 9;
        public const int MineCount = 10;

        private GameCell[,] _grid = new GameCell[Rows, Cols];
        private bool _gameOver;
        private bool _gameWon;
        private int _flagsPlaced;
        private bool _isGameStarted;
        private bool _isFlagMode;
        private string _statusMessage = "Good luck! 🎯";

        public List<GameCell> Cells { get; private set; } = new();

        public bool IsGameStarted
        {
            get => _isGameStarted;
            set { _isGameStarted = value; OnPropertyChanged(); }
        }

        public bool IsFlagMode
        {
            get => _isFlagMode;
            set { _isFlagMode = value; OnPropertyChanged(); }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public int MinesLeft => MineCount - _flagsPlaced;

        public ICommand RevealCommand { get; }
        public ICommand FlagCommand { get; }
        public ICommand RestartCommand { get; }
        public ICommand ToggleFlagModeCommand { get; }
        public ICommand StartGameCommand { get; }
        public ICommand QuitGameCommand { get; }

        public GameViewModel()
        {
            RevealCommand = new Command<GameCell>(RevealCell);
            FlagCommand = new Command<GameCell>(FlagCell);
            RestartCommand = new Command(InitGame);
            ToggleFlagModeCommand = new Command(ToggleFlagMode);
            StartGameCommand = new Command(StartGame);
            QuitGameCommand = new Command(QuitGame);
            // Don't call InitGame here, wait for user to start
        }

        public void InitGame()
        {
            _gameOver = false;
            _gameWon = false;
            _flagsPlaced = 0;
            IsFlagMode = false;
            StatusMessage = "Good luck! 🎯";
            _grid = new GameCell[Rows, Cols];

            for (int r = 0; r < Rows; r++)
                for (int c = 0; c < Cols; c++)
                    _grid[r, c] = new GameCell { Row = r, Column = c };

            var rng = new Random();
            int placed = 0;
            while (placed < MineCount)
            {
                int r = rng.Next(Rows);
                int c = rng.Next(Cols);
                if (!_grid[r, c].IsMine)
                {
                    _grid[r, c].IsMine = true;
                    placed++;
                }
            }

            for (int r = 0; r < Rows; r++)
                for (int c = 0; c < Cols; c++)
                    if (!_grid[r, c].IsMine)
                        _grid[r, c].AdjacentMines = CountAdjacentMines(r, c);

            Cells = _grid.Cast<MinesweeperApp.Models.GameCell>().ToList();
            OnPropertyChanged(nameof(Cells));
            OnPropertyChanged(nameof(MinesLeft));
        }

        private void ToggleFlagMode()
        {
            IsFlagMode = !IsFlagMode;
        }

        private void StartGame()
        {
            IsGameStarted = true;
            InitGame();
        }

        private void QuitGame()
        {
            Application.Current?.Quit();
        }

        private void RevealCell(GameCell cell)
        {
            if (_gameOver || _gameWon || cell.IsRevealed) return;

            if (IsFlagMode)
            {
                FlagCell(cell);
                return;
            }

            if (cell.IsFlagged) return;

            if (cell.IsMine)
            {
                cell.IsRevealed = true;
                RevealAllMines();
                _gameOver = true;
                StatusMessage = "💥 Boom! Game Over!";
                return;
            }

            FloodReveal(cell.Row, cell.Column);
            OnPropertyChanged(nameof(Cells));

            if (CheckWin())
            {
                _gameWon = true;
                StatusMessage = "🎉 You Win!";
            }
        }

        private void FlagCell(GameCell cell)
        {
            if (_gameOver || _gameWon || cell.IsRevealed) return;
            cell.IsFlagged = !cell.IsFlagged;
            _flagsPlaced += cell.IsFlagged ? 1 : -1;
            OnPropertyChanged(nameof(Cells));
            OnPropertyChanged(nameof(MinesLeft));
        }

        private void FloodReveal(int r, int c)
        {
            if (r < 0 || r >= Rows || c < 0 || c >= Cols) return;
            var cell = _grid[r, c];
            if (cell.IsRevealed || cell.IsFlagged || cell.IsMine) return;
            cell.IsRevealed = true;
            if (cell.AdjacentMines == 0)
                foreach (var (dr, dc) in Neighbors())
                    FloodReveal(r + dr, c + dc);
        }

        private void RevealAllMines()
        {
            foreach (var cell in Cells.Where(c => c.IsMine))
                cell.IsRevealed = true;
            OnPropertyChanged(nameof(Cells));
        }

        private bool CheckWin() =>
            Cells.Where(c => !c.IsMine).All(c => c.IsRevealed);

        private int CountAdjacentMines(int r, int c) =>
            Neighbors().Count(n =>
            {
                int nr = r + n.dr, nc = c + n.dc;
                return nr >= 0 && nr < Rows && nc >= 0 && nc < Cols && _grid[nr, nc].IsMine;
            });

        private static IEnumerable<(int dr, int dc)> Neighbors() =>
            new[] { (-1, -1), (-1, 0), (-1, 1), (0, -1), (0, 1), (1, -1), (1, 0), (1, 1) };

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class BoolToFlagTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? "🚩" : "💣";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? Color.FromArgb("#FF5722") : Color.FromArgb("#4CAF50");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToInstructionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? "Tap to flag/unflag 🚩" : "Tap to reveal 💣";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isVisible = (bool)value;
            if (parameter?.ToString() == "inverse")
                isVisible = !isVisible;
            return isVisible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

public class BoolToActionTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (bool)value ? "flag/unflag" : "reveal";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class BoolToInstructionConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (bool)value ? "Tap to flag/unflag 🚩" : "Tap to reveal 💣";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}