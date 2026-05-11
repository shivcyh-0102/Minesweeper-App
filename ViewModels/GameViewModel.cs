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
        private GameSettings? _currentSettings;
        private int _rows;
        private int _cols;
        private int _mineCount;
        private GameCell[,]? _grid;
        private bool _gameOver;
        private bool _gameWon;
        private int _flagsPlaced;
        private bool _isGameStarted;
        private bool _isFlagMode;
        private string _statusMessage = "Select Difficulty 🎯";
        private DifficultyLevel _selectedDifficulty = DifficultyLevel.Easy;
        private bool _isDifficultySelectionVisible = true;
        private int _elapsedSeconds;
        private IDispatcherTimer? _gameTimer;
        private string _bestTimeEasy = "--:--";
        private string _bestTimeMedium = "--:--";
        private string _bestTimeHard = "--:--";

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

        public DifficultyLevel SelectedDifficulty
        {
            get => _selectedDifficulty;
            set { _selectedDifficulty = value; OnPropertyChanged(); }
        }

        public bool IsDifficultySelectionVisible
        {
            get => _isDifficultySelectionVisible;
            set { _isDifficultySelectionVisible = value; OnPropertyChanged(); }
        }

        public int ElapsedSeconds
        {
            get => _elapsedSeconds;
            set { _elapsedSeconds = value; OnPropertyChanged(nameof(FormattedTime)); }
        }

        public string FormattedTime
        {
            get
            {
                int minutes = ElapsedSeconds / 60;
                int seconds = ElapsedSeconds % 60;
                return $"{minutes:D2}:{seconds:D2}";
            }
        }

        public string BestTimeEasy
        {
            get => _bestTimeEasy;
            set { _bestTimeEasy = value; OnPropertyChanged(); }
        }

        public string BestTimeMedium
        {
            get => _bestTimeMedium;
            set { _bestTimeMedium = value; OnPropertyChanged(); }
        }

        public string BestTimeHard
        {
            get => _bestTimeHard;
            set { _bestTimeHard = value; OnPropertyChanged(); }
        }

        public int MinesLeft => _mineCount - _flagsPlaced;

        public ICommand RevealCommand { get; }
        public ICommand FlagCommand { get; }
        public ICommand RestartCommand { get; }
        public ICommand ToggleFlagModeCommand { get; }
        public ICommand StartGameCommand { get; }
        public ICommand QuitGameCommand { get; }
        public ICommand SelectDifficultyCommand { get; }
        public ICommand BackToDifficultyCommand { get; }

        public GameViewModel()
        {
            RevealCommand = new Command<GameCell>(RevealCell);
            FlagCommand = new Command<GameCell>(FlagCell);
            RestartCommand = new Command(RestartCurrentGame);
            ToggleFlagModeCommand = new Command(ToggleFlagMode);
            StartGameCommand = new Command(StartGame);
            QuitGameCommand = new Command(QuitGame);
            SelectDifficultyCommand = new Command<DifficultyLevel>(SelectDifficulty);
            BackToDifficultyCommand = new Command(BackToDifficulty);

            LoadBestTimes();
        }

        private void LoadBestTimes()
        {
            BestTimeEasy = FormatStoredTime(Preferences.Default.Get("best_time_easy", -1));
            BestTimeMedium = FormatStoredTime(Preferences.Default.Get("best_time_medium", -1));
            BestTimeHard = FormatStoredTime(Preferences.Default.Get("best_time_hard", -1));
        }

        private string FormatStoredTime(int seconds)
        {
            if (seconds < 0) return "--:--";
            int minutes = seconds / 60;
            int secs = seconds % 60;
            return $"{minutes:D2}:{secs:D2}";
        }

        private void SaveBestTime(int seconds)
        {
            string prefsKey = SelectedDifficulty switch
            {
                DifficultyLevel.Easy => "best_time_easy",
                DifficultyLevel.Medium => "best_time_medium",
                DifficultyLevel.Hard => "best_time_hard",
                _ => "best_time_easy"
            };

            int currentBest = Preferences.Default.Get(prefsKey, -1);
            if (currentBest < 0 || seconds < currentBest)
            {
                Preferences.Default.Set(prefsKey, seconds);
                LoadBestTimes();
            }
        }

        private void SelectDifficulty(DifficultyLevel difficulty)
        {
            SelectedDifficulty = difficulty;
            StartGame();
        }

        private void BackToDifficulty()
        {
            StopGameTimer();
            IsGameStarted = false;
            IsDifficultySelectionVisible = true;
            StatusMessage = "Select Difficulty 🎯";
        }

        public void InitGame()
        {
            _currentSettings = GameSettings.GetSettings(SelectedDifficulty);
            _rows = _currentSettings.Rows;
            _cols = _currentSettings.Cols;
            _mineCount = _currentSettings.MineCount;

            _gameOver = false;
            _gameWon = false;
            _flagsPlaced = 0;
            IsFlagMode = false;
            ElapsedSeconds = 0;
            StatusMessage = "Good luck! 🎯";
            _grid = new GameCell[_rows, _cols];

            for (int r = 0; r < _rows; r++)
                for (int c = 0; c < _cols; c++)
                    _grid[r, c] = new GameCell { Row = r, Column = c };

            var rng = new Random();
            int placed = 0;
            while (placed < _mineCount)
            {
                int r = rng.Next(_rows);
                int c = rng.Next(_cols);
                if (!_grid[r, c].IsMine)
                {
                    _grid[r, c].IsMine = true;
                    placed++;
                }
            }

            for (int r = 0; r < _rows; r++)
                for (int c = 0; c < _cols; c++)
                    if (!_grid[r, c].IsMine)
                        _grid[r, c].AdjacentMines = CountAdjacentMines(r, c);

            Cells = _grid.Cast<GameCell>().ToList();
            OnPropertyChanged(nameof(Cells));
            OnPropertyChanged(nameof(MinesLeft));

            StartGameTimer();
        }

        private void StartGameTimer()
        {
            if (_gameTimer != null)
                StopGameTimer();

            var dispatcher = Application.Current?.Dispatcher ?? Dispatcher.GetForCurrentThread();
            if (dispatcher == null) return;

            _gameTimer = dispatcher.CreateTimer();
            _gameTimer.Interval = TimeSpan.FromSeconds(1);
            _gameTimer.Tick += (s, e) =>
            {
                ElapsedSeconds++;
            };
            _gameTimer.Start();
        }

        private void StopGameTimer()
        {
            if (_gameTimer != null)
            {
                _gameTimer.Stop();
                _gameTimer = null;
            }
        }

        private void ToggleFlagMode()
        {
            IsFlagMode = !IsFlagMode;
        }

        private void StartGame()
        {
            IsGameStarted = true;
            IsDifficultySelectionVisible = false;
            InitGame();
        }

        private void RestartCurrentGame()
        {
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
                StopGameTimer();
                return;
            }

            FloodReveal(cell.Row, cell.Column);
            OnPropertyChanged(nameof(Cells));

            if (CheckWin())
            {
                _gameWon = true;
                StatusMessage = "🎉 You Win!";
                StopGameTimer();
                SaveBestTime(ElapsedSeconds);
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
            if (_grid == null || r < 0 || r >= _rows || c < 0 || c >= _cols) return;
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

        private int CountAdjacentMines(int r, int c)
        {
            if (_grid == null) return 0;
            return Neighbors().Count(n =>
            {
                int nr = r + n.dr, nc = c + n.dc;
                return nr >= 0 && nr < _rows && nc >= 0 && nc < _cols && _grid[nr, nc].IsMine;
            });
        }

        private static IEnumerable<(int dr, int dc)> Neighbors() =>
            new[] { (-1, -1), (-1, 0), (-1, 1), (0, -1), (0, 1), (1, -1), (1, 0), (1, 1) };

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class BoolToFlagTextConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return (bool?)value == true ? "🚩" : "💣";
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return (bool?)value == true ? Color.FromArgb("#FF5722") : Color.FromArgb("#4CAF50");
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToInstructionConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return (bool?)value == true ? "Tap to flag/unflag 🚩" : "Tap to reveal 💣";
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            bool isVisible = (bool?)value == true;
            if (parameter?.ToString() == "inverse")
                isVisible = !isVisible;
            return isVisible;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToActionTextConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return (bool?)value == true ? "flag/unflag" : "reveal";
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}